using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;
using Xunit;

namespace RoadbikeCare.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        string? configuredConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RoadbikeCare");
        try
        {
            // Empty environment values fall through to user secrets; whitespace forces the in-memory provider.
            Environment.SetEnvironmentVariable("ConnectionStrings__RoadbikeCare", " ");
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureTestServices(services => services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                        options.DefaultScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
            });
            _client = _factory.CreateClient();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__RoadbikeCare", configuredConnectionString);
        }
    }

    [Fact]
    public async Task HealthAndResourceRoutes_ReturnSuccessfulResponses()
    {
        using var healthResponse = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        using var health = JsonDocument.Parse(await healthResponse.Content.ReadAsStringAsync());
        Assert.Equal("ok", health.RootElement.GetProperty("status").GetString());
        Assert.Equal("development-in-memory", health.RootElement.GetProperty("storage").GetString());

        using var swaggerUiResponse = await _client.GetAsync("/swagger/index.html");
        using var swaggerJsonResponse = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swaggerUiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, swaggerJsonResponse.StatusCode);
        using var swagger = JsonDocument.Parse(await swaggerJsonResponse.Content.ReadAsStringAsync());
        Assert.Equal("Roadbike Care API", swagger.RootElement.GetProperty("info").GetProperty("title").GetString());

        foreach (var path in new[]
        {
            "/", "/api", "/api/customers", "/api/bikes", "/api/shops", "/api/staff",
            "/api/plans", "/api/contracts", "/api/overhauls", "/api/maintenance", "/api/events", "/api/history"
        })
        {
            using var response = await _client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ProtectedRoutes_RequireAuthentication()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        using var protectedResponse = await _client.GetAsync("/api/bikes");
        using var healthResponse = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }

    [Fact]
    public async Task StaffRole_CanReadOperationsButCannotManageStaffAccounts()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Staff");
        using var meJson = JsonDocument.Parse(await _client.GetStringAsync("/api/me"));
        Assert.Equal("staff", meJson.RootElement.GetProperty("role").GetString());
        using var bikesResponse = await _client.GetAsync("/api/bikes");
        using var shopsResponse = await _client.GetAsync("/api/shops");
        using var staffResponse = await _client.GetAsync("/api/staff");
        using var deniedStaffWrite = await _client.PostAsJsonAsync("/api/staff",
            new StaffMember(0, 1, "Unauthorized Staff", "受付担当"));
        Assert.Equal(HttpStatusCode.OK, bikesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, shopsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedStaffWrite.StatusCode);
    }

    [Fact]
    public async Task CreateBike_AllowsMultipleMissingSerialNumbersAndTrimsSerial()
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Missing Serial Test", $"missing-serial-{Guid.NewGuid():N}@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        foreach (var serial in new[] { "", "   " })
        {
            using var bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
                new Bike(0, customerId, "Test", "No Serial", 2026, serial));
            Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
            using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
            Assert.Equal("", bikeJson.RootElement.GetProperty("serial").GetString());
        }

        using var identifiedBikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "With Serial", 2026, "  UNIQUE-123  "));
        Assert.Equal(HttpStatusCode.Created, identifiedBikeResponse.StatusCode);
        using var identifiedBikeJson = JsonDocument.Parse(await identifiedBikeResponse.Content.ReadAsStringAsync());
        Assert.Equal("UNIQUE-123", identifiedBikeJson.RootElement.GetProperty("serial").GetString());
    }

    [Fact]
    public void BikeSerialIndex_OnlyEnforcesUniquenessForNonEmptySerials()
    {
        var options = new DbContextOptionsBuilder<RoadbikeCareDbContext>()
            .UseSqlServer("Server=localhost;Database=metadata-only;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new RoadbikeCareDbContext(options);
        var serialIndex = db.Model.FindEntityType(typeof(Bike))!.GetIndexes()
            .Single(index => index.Properties.Single().Name == nameof(Bike.Serial));

        Assert.True(serialIndex.IsUnique);
        Assert.Equal("[Serial] <> N''", serialIndex.GetFilter());
    }

    [Fact]
    public async Task CompletingOverhaul_ConsumesContractAndAddsHistoryOnlyOnce()
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Integration Test", "integration@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        var bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Road", 2026, $"TEST-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        var contractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 3, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);

        var workResponse = await _client.PostAsJsonAsync("/api/overhauls",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 1, 10), null, "Integration overhaul", "Test details", 1, 1));
        Assert.Equal(HttpStatusCode.Created, workResponse.StatusCode);
        using var workJson = JsonDocument.Parse(await workResponse.Content.ReadAsStringAsync());
        Assert.Equal("案内待ち", workJson.RootElement.GetProperty("status").GetString());
        var workId = workJson.RootElement.GetProperty("id").GetInt32();

        var report = new CompleteWorkReportRequest(
            90,
            [new WorkChecklistItem("ブレーキ", "調整", "パッド残量確認")],
            [new WorkReportPart("ブレーキパッド", 2, "個")]);
        var invalidReport = new CompleteWorkRequest(
            new DateOnly(2028, 1, 12),
            "integration-test",
            report with { WorkMinutes = 0 });
        using var invalidReportResponse = await _client.PostAsJsonAsync(
            $"/api/work/{workId}/complete", invalidReport);
        Assert.Equal(HttpStatusCode.BadRequest, invalidReportResponse.StatusCode);

        var completeRequest = new CompleteWorkRequest(
            new DateOnly(2028, 1, 12), "integration-test", report);
        var completedResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/complete", completeRequest);
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        using var completedJson = JsonDocument.Parse(await completedResponse.Content.ReadAsStringAsync());
        Assert.Equal("完了", completedJson.RootElement.GetProperty("status").GetString());

        using var contractsJson = JsonDocument.Parse(await _client.GetStringAsync("/api/contracts"));
        var savedContract = contractsJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("bikeId").GetInt32() == bikeId);
        Assert.Equal(1, savedContract.GetProperty("overhaulUsed").GetInt32());

        using var historyJson = JsonDocument.Parse(await _client.GetStringAsync("/api/history"));
        var savedHistory = historyJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("bikeId").GetInt32() == bikeId);
        var savedReport = savedHistory.GetProperty("report");
        Assert.Equal(90, savedReport.GetProperty("workMinutes").GetInt32());
        Assert.Equal("ブレーキ", savedReport.GetProperty("checklist")[0].GetProperty("item").GetString());
        Assert.Equal(2m, savedReport.GetProperty("parts")[0].GetProperty("quantity").GetDecimal());
        Assert.False(string.IsNullOrWhiteSpace(savedReport.GetProperty("shopName").GetString()));

        using var duplicateResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/complete", completeRequest);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task ContractCanRenewForSameBikeAfterPriorContractEnds_ButCannotOverlap()
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Renewal Test", "renewal@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        var bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Renewal", 2026, $"RENEW-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        var firstContractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, firstContractResponse.StatusCode);

        var overlappingContractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2027, 11, 20), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.BadRequest, overlappingContractResponse.StatusCode);

        var renewalResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2028, 11, 15), new DateOnly(2028, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, renewalResponse.StatusCode);

        using var contractsJson = JsonDocument.Parse(await _client.GetStringAsync("/api/contracts"));
        var bikeContracts = contractsJson.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("bikeId").GetInt32() == bikeId)
            .ToArray();
        Assert.Equal(2, bikeContracts.Length);
        Assert.False(bikeContracts.Single(item => item.GetProperty("startDate").GetDateTime() == new DateTime(2027, 12, 1))
            .GetProperty("isActive").GetBoolean());
        Assert.True(bikeContracts.Single(item => item.GetProperty("startDate").GetDateTime() == new DateTime(2028, 12, 1))
            .GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task BikeProfile_UpdatePreservesContractAndComponentChangesKeepHistory()
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Bike Profile Test", "bike-profile@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        var serial = $"PROFILE-{Guid.NewGuid():N}";
        var bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Profile", 2024, serial, PurchaseDate: new DateOnly(2024, 5, 1)));
        Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        var contractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);

        using var updateBikeResponse = await _client.PutAsJsonAsync($"/api/bikes/{bikeId}",
            new Bike(bikeId, customerId, "Updated", "Profile", 2024, serial, PurchaseDate: new DateOnly(2024, 6, 1)));
        Assert.Equal(HttpStatusCode.OK, updateBikeResponse.StatusCode);
        using var updatedBikeResponse = await _client.GetAsync($"/api/bikes/{bikeId}");
        using var updatedBikeJson = JsonDocument.Parse(await updatedBikeResponse.Content.ReadAsStringAsync());
        Assert.Equal("2024-06-01", updatedBikeJson.RootElement.GetProperty("purchaseDate").GetString());
        Assert.False(updatedBikeJson.RootElement.GetProperty("activeContractId").ValueKind == JsonValueKind.Null);

        var firstComponentResponse = await _client.PostAsJsonAsync($"/api/bikes/{bikeId}/components",
            new CreateBikeComponentRequest("ドライブトレイン", "Shimano", "105", new DateOnly(2024, 6, 1)));
        Assert.Equal(HttpStatusCode.Created, firstComponentResponse.StatusCode);
        var replacementResponse = await _client.PostAsJsonAsync($"/api/bikes/{bikeId}/components",
            new CreateBikeComponentRequest("ドライブトレイン", "Shimano", "Ultegra", new DateOnly(2025, 1, 1)));
        Assert.Equal(HttpStatusCode.Created, replacementResponse.StatusCode);

        using var componentsResponse = await _client.GetAsync($"/api/bikes/{bikeId}/components");
        using var componentsJson = JsonDocument.Parse(await componentsResponse.Content.ReadAsStringAsync());
        var components = componentsJson.RootElement.EnumerateArray().ToArray();
        Assert.Equal(2, components.Length);
        Assert.Equal("2025-01-01", components.Single(item => item.GetProperty("model").GetString() == "105")
            .GetProperty("removedDate").GetString());
        Assert.Equal(JsonValueKind.Null, components.Single(item => item.GetProperty("model").GetString() == "Ultegra")
            .GetProperty("removedDate").ValueKind);
    }

    [Fact]
    public async Task AdHocWorkAndInspection_CanBeStoppedWithoutConsumingContractUsage()
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Ad Hoc Test", "adhoc@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        var bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Road", 2026, $"ADHOC-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        var contractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);

        var workResponse = await _client.PostAsJsonAsync("/api/maintenance",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 4, 10), null, "突発メンテナンス", "確認作業", 1, 1, true));
        Assert.Equal(HttpStatusCode.Created, workResponse.StatusCode);
        using var workJson = JsonDocument.Parse(await workResponse.Content.ReadAsStringAsync());
        var workId = workJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("作業中", workJson.RootElement.GetProperty("status").GetString());
        Assert.True(workJson.RootElement.GetProperty("isAdHoc").GetBoolean());

        var eventResponse = await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest(bikeId, "目標イベント", new DateOnly(2028, 5, 10), new DateOnly(2028, 5, 8),
                null, "点検メモ", 1, 1, true));
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        using var eventJson = JsonDocument.Parse(await eventResponse.Content.ReadAsStringAsync());
        var eventId = eventJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("作業中", eventJson.RootElement.GetProperty("status").GetString());
        Assert.True(eventJson.RootElement.GetProperty("isAdHoc").GetBoolean());

        var stoppedDate = new DateOnly(2028, 5, 9);
        var stopRequest = new StopWorkRequest(stoppedDate, "顧客都合");
        using var stoppedWorkResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/stop", stopRequest);
        using var stoppedEventResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/stop", stopRequest);
        Assert.Equal(HttpStatusCode.OK, stoppedWorkResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stoppedEventResponse.StatusCode);
        using var stoppedWorkJson = JsonDocument.Parse(await stoppedWorkResponse.Content.ReadAsStringAsync());
        using var stoppedEventJson = JsonDocument.Parse(await stoppedEventResponse.Content.ReadAsStringAsync());
        Assert.Equal("作業中止", stoppedWorkJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("作業中止", stoppedEventJson.RootElement.GetProperty("status").GetString());
        using var duplicateStopResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/stop", stopRequest);
        using var stoppedCompletionResponse = await _client.PostAsJsonAsync(
            $"/api/events/{eventId}/complete",
            new CompleteWorkRequest(stoppedDate, "中止後の完了は不可"));
        Assert.Equal(HttpStatusCode.BadRequest, duplicateStopResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, stoppedCompletionResponse.StatusCode);

        using var historyJson = JsonDocument.Parse(await _client.GetStringAsync("/api/history"));
        var stoppedHistory = historyJson.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("category").GetString() == "作業中止")
            .ToArray();
        Assert.Equal(2, stoppedHistory.Length);
        Assert.All(stoppedHistory, item => Assert.True(item.GetProperty("isAdHoc").GetBoolean()));
        Assert.All(stoppedHistory, item => Assert.Equal("顧客都合", item.GetProperty("memo").GetString()));

        using var contractsJson = JsonDocument.Parse(await _client.GetStringAsync("/api/contracts"));
        var savedContract = contractsJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("bikeId").GetInt32() == bikeId);
        Assert.Equal(0, savedContract.GetProperty("maintenanceUsed").GetInt32());
        Assert.Equal(0, savedContract.GetProperty("inspectionUsed").GetInt32());

        var secondWorkResponse = await _client.PostAsJsonAsync("/api/maintenance",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 6, 1), null, "再受付", "新規作業", 1, 1));
        Assert.Equal(HttpStatusCode.Created, secondWorkResponse.StatusCode);

        var completedEventResponse = await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest(bikeId, "完了点検", new DateOnly(2028, 6, 10), new DateOnly(2028, 6, 8),
                null, "点検内容", 1, 1));
        Assert.Equal(HttpStatusCode.Created, completedEventResponse.StatusCode);
        using var completedEventJson = JsonDocument.Parse(await completedEventResponse.Content.ReadAsStringAsync());
        var completedEventId = completedEventJson.RootElement.GetProperty("id").GetInt32();
        var eventReport = new CompleteWorkReportRequest(45, [], []);
        using var completedEventResponseBody = await _client.PostAsJsonAsync(
            $"/api/events/{completedEventId}/complete",
            new CompleteWorkRequest(new DateOnly(2028, 6, 8), "点検完了", eventReport));
        Assert.Equal(HttpStatusCode.OK, completedEventResponseBody.StatusCode);

        using var completedHistoryJson = JsonDocument.Parse(await _client.GetStringAsync("/api/history"));
        var completedEventHistory = completedHistoryJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("title").GetString() == "完了点検");
        Assert.Equal(45, completedEventHistory.GetProperty("report").GetProperty("workMinutes").GetInt32());
    }

    [Fact]
    public async Task CustomerIdentity_RestrictsReadsToLinkedCustomerAndDeniesWrites()
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Scoped Customer", "scoped@example.test", "000"));
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        var bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Scoped", 2026, $"SCOPE-{Guid.NewGuid():N}"));
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        var contractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 3, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        var workResponse = await _client.PostAsJsonAsync("/api/maintenance",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 4, 10), null, "Scoped maintenance", "Test details", 1, 1));
        Assert.Equal(HttpStatusCode.Created, workResponse.StatusCode);
        using var workJson = JsonDocument.Parse(await workResponse.Content.ReadAsStringAsync());
        var workId = workJson.RootElement.GetProperty("id").GetInt32();
        var completionResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/complete",
            new CompleteWorkRequest(new DateOnly(2028, 4, 10), "scoped-test"));
        Assert.Equal(HttpStatusCode.OK, completionResponse.StatusCode);
        var eventResponse = await _client.PostAsJsonAsync("/api/events", new CreateEventRequest(
            bikeId, "Scoped event", new DateOnly(2028, 5, 1), new DateOnly(2028, 4, 28), null, "", 1, 1));
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        using var eventJson = JsonDocument.Parse(await eventResponse.Content.ReadAsStringAsync());
        Assert.Equal("案内待ち", eventJson.RootElement.GetProperty("status").GetString());

        using var linkResponse = await _client.PutAsJsonAsync($"/api/customers/{customerId}/identity",
            new CustomerIdentityRequest("external-tenant", "customer-object"));
        Assert.Equal(HttpStatusCode.NoContent, linkResponse.StatusCode);

        using var updateCustomerResponse = await _client.PutAsJsonAsync($"/api/customers/{customerId}",
            new Customer(customerId, "Scoped Customer", "updated@example.test", "123-456"));
        Assert.Equal(HttpStatusCode.OK, updateCustomerResponse.StatusCode);

        var duplicateCustomerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Duplicate Link", "duplicate@example.test", "000"));
        using var duplicateCustomerJson = JsonDocument.Parse(await duplicateCustomerResponse.Content.ReadAsStringAsync());
        var duplicateCustomerId = duplicateCustomerJson.RootElement.GetProperty("id").GetInt32();
        using var duplicateLinkResponse = await _client.PutAsJsonAsync($"/api/customers/{duplicateCustomerId}/identity",
            new CustomerIdentityRequest("external-tenant", "customer-object"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateLinkResponse.StatusCode);

        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");
        _client.DefaultRequestHeaders.Add("X-Test-TenantId", "external-tenant");
        _client.DefaultRequestHeaders.Add("X-Test-ObjectId", "customer-object");

        using var meJson = JsonDocument.Parse(await _client.GetStringAsync("/api/me"));
        Assert.Equal("customer", meJson.RootElement.GetProperty("role").GetString());
        Assert.Equal(customerId, meJson.RootElement.GetProperty("customerId").GetInt32());
        using var updatedCustomerJson = JsonDocument.Parse(await _client.GetStringAsync($"/api/customers/{customerId}"));
        Assert.Equal("updated@example.test", updatedCustomerJson.RootElement.GetProperty("email").GetString());
        Assert.Equal("123-456", updatedCustomerJson.RootElement.GetProperty("phone").GetString());

        await AssertOwnedCollectionAsync("/api/customers", "id", customerId);
        await AssertOwnedCollectionAsync("/api/bikes", "id", bikeId);
        await AssertOwnedCollectionAsync("/api/contracts", "bikeId", bikeId);
        await AssertOwnedCollectionAsync("/api/maintenance", "bikeId", bikeId);
        await AssertOwnedCollectionAsync("/api/events", "bikeId", bikeId);
        await AssertOwnedCollectionAsync("/api/history", "bikeId", bikeId);
        using var deniedShops = await _client.GetAsync("/api/shops");
        using var deniedStaff = await _client.GetAsync("/api/staff");
        Assert.Equal(HttpStatusCode.Forbidden, deniedShops.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedStaff.StatusCode);

        using var foreignCustomer = await _client.GetAsync("/api/customers/1");
        using var foreignBike = await _client.GetAsync("/api/bikes/1");
        Assert.Equal(HttpStatusCode.NotFound, foreignCustomer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignBike.StatusCode);

        using var deniedWrite = await _client.PostAsJsonAsync("/api/maintenance",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 4, 11), null, "Denied", "", 1, 1));
        Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);
    }

    private async Task AssertOwnedCollectionAsync(string path, string bikeIdProperty, int expectedId)
    {
        using var json = JsonDocument.Parse(await _client.GetStringAsync(path));
        var items = json.RootElement.EnumerateArray().ToArray();
        Assert.NotEmpty(items);
        Assert.All(items, item => Assert.Equal(expectedId, item.GetProperty(bikeIdProperty).GetInt32()));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "IntegrationTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers["X-Test-Anonymous"] == "true")
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "StaffAdmin";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "integration-user"),
            new("name", "Integration Test"),
            new("roles", role)
        };
        if (Request.Headers.TryGetValue("X-Test-TenantId", out var tenantId)) claims.Add(new("tid", tenantId.ToString()));
        if (Request.Headers.TryGetValue("X-Test-ObjectId", out var objectId)) claims.Add(new("oid", objectId.ToString()));

        var identity = new ClaimsIdentity(claims, SchemeName, "name", "roles");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}