using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
        var configuredConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RoadbikeCare");
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
        using HttpResponseMessage healthResponse = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        using var health = JsonDocument.Parse(await healthResponse.Content.ReadAsStringAsync());
        Assert.Equal("ok", health.RootElement.GetProperty("status").GetString());
        Assert.Equal("development-in-memory", health.RootElement.GetProperty("storage").GetString());

        using HttpResponseMessage swaggerUiResponse = await _client.GetAsync("/swagger/index.html");
        using HttpResponseMessage swaggerJsonResponse = await _client.GetAsync("/swagger/v1/swagger.json");
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
            using HttpResponseMessage response = await _client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ProtectedRoutes_RequireAuthentication()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        using HttpResponseMessage protectedResponse = await _client.GetAsync("/api/bikes");
        using HttpResponseMessage healthResponse = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }

    [Fact]
    public async Task StaffRole_CanReadOperationsButCannotManageStaffAccounts()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Staff");
        using var meJson = JsonDocument.Parse(await _client.GetStringAsync("/api/me"));
        Assert.Equal("staff", meJson.RootElement.GetProperty("role").GetString());
        using HttpResponseMessage bikesResponse = await _client.GetAsync("/api/bikes");
        using HttpResponseMessage shopsResponse = await _client.GetAsync("/api/shops");
        using HttpResponseMessage staffResponse = await _client.GetAsync("/api/staff");
        using HttpResponseMessage deniedStaffWrite = await _client.PostAsJsonAsync("/api/staff",
            new StaffMember(0, 1, "Unauthorized Staff", "受付担当"));
        Assert.Equal(HttpStatusCode.OK, bikesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, shopsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, staffResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedStaffWrite.StatusCode);
    }

    [Fact]
    public async Task CustomerContacts_StaffCanRecordAndResolveFollowUps()
    {
        HttpResponseMessage customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Contact Test", $"contact-{Guid.NewGuid():N}@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();
        var followUpDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            $"/api/customers/{customerId}/contacts",
            new CreateCustomerContactRequest("電話", "点検後の状態を確認しました。", followUpDate, 1));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var contactId = createdJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("未対応", createdJson.RootElement.GetProperty("followUpStatus").GetString());
        Assert.Equal(followUpDate.ToString("yyyy-MM-dd"),
            createdJson.RootElement.GetProperty("followUpDate").GetString());
        Assert.Equal("田中", createdJson.RootElement.GetProperty("followUpStaffName").GetString());

        using var contactsJson = JsonDocument.Parse(
            await _client.GetStringAsync($"/api/customers/{customerId}/contacts"));
        Assert.Single(contactsJson.RootElement.EnumerateArray());
        using var followUpsJson = JsonDocument.Parse(
            await _client.GetStringAsync("/api/customer-contacts/follow-ups"));
        Assert.Contains(followUpsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == contactId);

        using HttpResponseMessage updateResponse = await _client.PutAsJsonAsync(
            $"/api/customers/{customerId}/contacts/{contactId}/follow-up",
            new UpdateCustomerFollowUpRequest("完了", "異音は再発していないとのこと。"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var updatedJson = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal("完了", updatedJson.RootElement.GetProperty("followUpStatus").GetString());
        Assert.Equal("異音は再発していないとのこと。",
            updatedJson.RootElement.GetProperty("followUpResult").GetString());
        using var resolvedFollowUpsJson = JsonDocument.Parse(
            await _client.GetStringAsync("/api/customer-contacts/follow-ups"));
        Assert.DoesNotContain(resolvedFollowUpsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == contactId);

        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Customer");
        using HttpResponseMessage deniedResponse = await _client.GetAsync(
            $"/api/customers/{customerId}/contacts");
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
    }

    [Fact]
    public async Task CreateBike_AllowsMultipleMissingSerialNumbersAndTrimsSerial()
    {
        HttpResponseMessage customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Missing Serial Test", $"missing-serial-{Guid.NewGuid():N}@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        foreach (var serial in new[] { "", "   " })
        {
            using HttpResponseMessage bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
                new Bike(0, customerId, "Test", "No Serial", 2026, serial));
            Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
            using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
            Assert.Equal("", bikeJson.RootElement.GetProperty("serial").GetString());
        }

        using HttpResponseMessage identifiedBikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "With Serial", 2026, "  UNIQUE-123  "));
        Assert.Equal(HttpStatusCode.Created, identifiedBikeResponse.StatusCode);
        using var identifiedBikeJson = JsonDocument.Parse(await identifiedBikeResponse.Content.ReadAsStringAsync());
        Assert.Equal("UNIQUE-123", identifiedBikeJson.RootElement.GetProperty("serial").GetString());
    }

    [Fact]
    public void BikeSerialIndex_OnlyEnforcesUniquenessForNonEmptySerials()
    {
        DbContextOptions<RoadbikeCareDbContext> options = new DbContextOptionsBuilder<RoadbikeCareDbContext>()
            .UseSqlServer("Server=localhost;Database=metadata-only;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new RoadbikeCareDbContext(options);
        IIndex serialIndex = db.Model.FindEntityType(typeof(Bike))!.GetIndexes()
            .Single(index => index.Properties.Single().Name == nameof(Bike.Serial));

        Assert.True(serialIndex.IsUnique);
        Assert.Equal("[Serial] <> N''", serialIndex.GetFilter());
    }

    [Fact]
    public async Task CompletingOverhaul_ConsumesContractAndAddsHistoryOnlyOnce()
    {
        HttpResponseMessage customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Integration Test", "integration@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Road", 2026, $"TEST-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage contractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 3, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);

        HttpResponseMessage invalidScheduleResponse = await _client.PostAsJsonAsync("/api/overhauls",
            new CreateWorkRequest(
                bikeId,
                new DateOnly(2028, 1, 10),
                new DateOnly(2028, 1, 11),
                "Invalid schedule",
                "Overlapping slots",
                1,
                1,
                EstimatedWorkMinutes: 90,
                ScheduleSlots:
                [
                    new(new DateOnly(2028, 1, 10), "09:00", "11:00"),
                    new(new DateOnly(2028, 1, 10), "10:00", "12:00")
                ]));
        Assert.Equal(HttpStatusCode.BadRequest, invalidScheduleResponse.StatusCode);

        HttpResponseMessage workResponse = await _client.PostAsJsonAsync("/api/overhauls",
            new CreateWorkRequest(
                bikeId,
                new DateOnly(2028, 1, 10),
                new DateOnly(2028, 1, 11),
                "Integration overhaul",
                "Test details",
                1,
                1,
                EstimatedWorkMinutes: 180,
                ScheduleSlots:
                [
                    new(new DateOnly(2028, 1, 10), "09:00", "11:00"),
                    new(new DateOnly(2028, 1, 11), "13:00", "14:00")
                ]));
        Assert.Equal(HttpStatusCode.Created, workResponse.StatusCode);
        using var workJson = JsonDocument.Parse(await workResponse.Content.ReadAsStringAsync());
        Assert.Equal("案内待ち", workJson.RootElement.GetProperty("status").GetString());
        Assert.Equal(180, workJson.RootElement.GetProperty("estimatedWorkMinutes").GetInt32());
        JsonElement scheduleSlots = workJson.RootElement.GetProperty("scheduleSlots");
        Assert.Equal(2, scheduleSlots.GetArrayLength());
        Assert.Equal("2028-01-10", scheduleSlots[0].GetProperty("date").GetString());
        Assert.Equal("09:00", scheduleSlots[0].GetProperty("startTime").GetString());
        var workId = workJson.RootElement.GetProperty("id").GetInt32();

        var updateRequest = new UpdateWorkRequest(
            new DateOnly(2028, 1, 10),
            new DateOnly(2028, 1, 11),
            "案内待ち",
            "Updated overhaul",
            "Updated details",
            1,
            EstimatedWorkMinutes: 210,
            ScheduleSlots: [new(new DateOnly(2028, 1, 11), "14:00", "15:00")]);
        using HttpResponseMessage updateResponse = await _client.PutAsJsonAsync($"/api/work/{workId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var updatedWorkJson = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal(210, updatedWorkJson.RootElement.GetProperty("estimatedWorkMinutes").GetInt32());
        Assert.Equal(
            "14:00",
            updatedWorkJson.RootElement.GetProperty("scheduleSlots")[0].GetProperty("startTime").GetString());

        var report = new CompleteWorkReportRequest(
            90,
            [new WorkChecklistItem("ブレーキ", "調整", "パッド残量確認")],
            [new WorkReportPart("ブレーキパッド", 2, "個")]);
        var invalidReport = new CompleteWorkRequest(
            new DateOnly(2028, 1, 12),
            "integration-test",
            report with { WorkMinutes = 0 });
        using HttpResponseMessage invalidReportResponse = await _client.PostAsJsonAsync(
            $"/api/work/{workId}/complete", invalidReport);
        Assert.Equal(HttpStatusCode.BadRequest, invalidReportResponse.StatusCode);

        var completeRequest = new CompleteWorkRequest(
            new DateOnly(2028, 1, 12), "integration-test", report);
        HttpResponseMessage completedResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/complete", completeRequest);
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        using var completedJson = JsonDocument.Parse(await completedResponse.Content.ReadAsStringAsync());
        Assert.Equal("完了", completedJson.RootElement.GetProperty("status").GetString());

        using var contractsJson = JsonDocument.Parse(await _client.GetStringAsync("/api/contracts"));
        JsonElement savedContract = contractsJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("bikeId").GetInt32() == bikeId);
        Assert.Equal(1, savedContract.GetProperty("overhaulUsed").GetInt32());

        using var historyJson = JsonDocument.Parse(await _client.GetStringAsync("/api/history"));
        JsonElement savedHistory = historyJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("bikeId").GetInt32() == bikeId);
        JsonElement savedReport = savedHistory.GetProperty("report");
        Assert.Equal(90, savedReport.GetProperty("workMinutes").GetInt32());
        Assert.Equal("ブレーキ", savedReport.GetProperty("checklist")[0].GetProperty("item").GetString());
        Assert.Equal(2m, savedReport.GetProperty("parts")[0].GetProperty("quantity").GetDecimal());
        Assert.False(string.IsNullOrWhiteSpace(savedReport.GetProperty("shopName").GetString()));
        Assert.Equal(1, savedReport.GetProperty("shopId").GetInt32());
        Assert.Equal(1, savedReport.GetProperty("receptionistId").GetInt32());

        using HttpResponseMessage duplicateResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/complete", completeRequest);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task ContractCanRenewForSameBikeAfterPriorContractEnds_ButCannotOverlap()
    {
        HttpResponseMessage customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Renewal Test", "renewal@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Renewal", 2026, $"RENEW-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage firstContractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, firstContractResponse.StatusCode);

        HttpResponseMessage overlappingContractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2027, 11, 20), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.BadRequest, overlappingContractResponse.StatusCode);

        HttpResponseMessage renewalResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2028, 11, 15), new DateOnly(2028, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, renewalResponse.StatusCode);

        using var contractsJson = JsonDocument.Parse(await _client.GetStringAsync("/api/contracts"));
        JsonElement[] bikeContracts = contractsJson.RootElement.EnumerateArray()
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
        HttpResponseMessage customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Bike Profile Test", "bike-profile@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        var serial = $"PROFILE-{Guid.NewGuid():N}";
        HttpResponseMessage bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Profile", 2024, serial, PurchaseDate: new DateOnly(2024, 5, 1)));
        Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage contractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);

        using HttpResponseMessage updateBikeResponse = await _client.PutAsJsonAsync($"/api/bikes/{bikeId}",
            new Bike(bikeId, customerId, "Updated", "Profile", 2024, serial, PurchaseDate: new DateOnly(2024, 6, 1)));
        Assert.Equal(HttpStatusCode.OK, updateBikeResponse.StatusCode);
        using HttpResponseMessage updatedBikeResponse = await _client.GetAsync($"/api/bikes/{bikeId}");
        using var updatedBikeJson = JsonDocument.Parse(await updatedBikeResponse.Content.ReadAsStringAsync());
        Assert.Equal("2024-06-01", updatedBikeJson.RootElement.GetProperty("purchaseDate").GetString());
        Assert.False(updatedBikeJson.RootElement.GetProperty("activeContractId").ValueKind == JsonValueKind.Null);

        HttpResponseMessage firstComponentResponse = await _client.PostAsJsonAsync($"/api/bikes/{bikeId}/components",
            new CreateBikeComponentRequest("ドライブトレイン", "Shimano", "105", new DateOnly(2024, 6, 1)));
        Assert.Equal(HttpStatusCode.Created, firstComponentResponse.StatusCode);
        HttpResponseMessage replacementResponse = await _client.PostAsJsonAsync($"/api/bikes/{bikeId}/components",
            new CreateBikeComponentRequest("ドライブトレイン", "Shimano", "Ultegra", new DateOnly(2025, 1, 1)));
        Assert.Equal(HttpStatusCode.Created, replacementResponse.StatusCode);

        using HttpResponseMessage componentsResponse = await _client.GetAsync($"/api/bikes/{bikeId}/components");
        using var componentsJson = JsonDocument.Parse(await componentsResponse.Content.ReadAsStringAsync());
        JsonElement[] components = componentsJson.RootElement.EnumerateArray().ToArray();
        Assert.Equal(2, components.Length);
        Assert.Equal("2025-01-01", components.Single(item => item.GetProperty("model").GetString() == "105")
            .GetProperty("removedDate").GetString());
        Assert.Equal(JsonValueKind.Null, components.Single(item => item.GetProperty("model").GetString() == "Ultegra")
            .GetProperty("removedDate").ValueKind);
    }

    [Fact]
    public async Task AdHocWorkAndInspection_CanBeStoppedWithoutConsumingContractUsage()
    {
        HttpResponseMessage customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Ad Hoc Test", "adhoc@example.test", "000"));
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Road", 2026, $"ADHOC-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, bikeResponse.StatusCode);
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage contractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 1, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);

        HttpResponseMessage workResponse = await _client.PostAsJsonAsync("/api/maintenance",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 4, 10), null, "突発メンテナンス", "確認作業", 1, 1, true));
        Assert.Equal(HttpStatusCode.Created, workResponse.StatusCode);
        using var workJson = JsonDocument.Parse(await workResponse.Content.ReadAsStringAsync());
        var workId = workJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("作業中", workJson.RootElement.GetProperty("status").GetString());
        Assert.True(workJson.RootElement.GetProperty("isAdHoc").GetBoolean());

        HttpResponseMessage eventResponse = await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest(bikeId, "目標イベント", new DateOnly(2028, 5, 10), new DateOnly(2028, 5, 8),
                null, "点検メモ", 1, 1, true));
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        using var eventJson = JsonDocument.Parse(await eventResponse.Content.ReadAsStringAsync());
        var eventId = eventJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("作業中", eventJson.RootElement.GetProperty("status").GetString());
        Assert.True(eventJson.RootElement.GetProperty("isAdHoc").GetBoolean());

        var stoppedDate = new DateOnly(2028, 5, 9);
        var stopRequest = new StopWorkRequest(stoppedDate, "顧客都合");
        using HttpResponseMessage stoppedWorkResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/stop", stopRequest);
        using HttpResponseMessage stoppedEventResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/stop", stopRequest);
        Assert.Equal(HttpStatusCode.OK, stoppedWorkResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stoppedEventResponse.StatusCode);
        using var stoppedWorkJson = JsonDocument.Parse(await stoppedWorkResponse.Content.ReadAsStringAsync());
        using var stoppedEventJson = JsonDocument.Parse(await stoppedEventResponse.Content.ReadAsStringAsync());
        Assert.Equal("作業中止", stoppedWorkJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("作業中止", stoppedEventJson.RootElement.GetProperty("status").GetString());
        using HttpResponseMessage duplicateStopResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/stop", stopRequest);
        using HttpResponseMessage stoppedCompletionResponse = await _client.PostAsJsonAsync(
            $"/api/events/{eventId}/complete",
            new CompleteWorkRequest(stoppedDate, "中止後の完了は不可"));
        Assert.Equal(HttpStatusCode.BadRequest, duplicateStopResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, stoppedCompletionResponse.StatusCode);

        using var historyJson = JsonDocument.Parse(await _client.GetStringAsync("/api/history"));
        JsonElement[] stoppedHistory = historyJson.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("category").GetString() == "作業中止")
            .ToArray();
        Assert.Equal(2, stoppedHistory.Length);
        Assert.All(stoppedHistory, item => Assert.True(item.GetProperty("isAdHoc").GetBoolean()));
        Assert.All(stoppedHistory, item => Assert.Equal("顧客都合", item.GetProperty("memo").GetString()));

        using var contractsJson = JsonDocument.Parse(await _client.GetStringAsync("/api/contracts"));
        JsonElement savedContract = contractsJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("bikeId").GetInt32() == bikeId);
        Assert.Equal(0, savedContract.GetProperty("maintenanceUsed").GetInt32());
        Assert.Equal(0, savedContract.GetProperty("inspectionUsed").GetInt32());

        HttpResponseMessage secondWorkResponse = await _client.PostAsJsonAsync("/api/maintenance",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 6, 1), null, "再受付", "新規作業", 1, 1));
        Assert.Equal(HttpStatusCode.Created, secondWorkResponse.StatusCode);

        HttpResponseMessage completedEventResponse = await _client.PostAsJsonAsync("/api/events",
            new CreateEventRequest(bikeId, "完了点検", new DateOnly(2028, 6, 10), new DateOnly(2028, 6, 8),
                null, "点検内容", 1, 1));
        Assert.Equal(HttpStatusCode.Created, completedEventResponse.StatusCode);
        using var completedEventJson = JsonDocument.Parse(await completedEventResponse.Content.ReadAsStringAsync());
        var completedEventId = completedEventJson.RootElement.GetProperty("id").GetInt32();
        var eventReport = new CompleteWorkReportRequest(45, [], []);
        using HttpResponseMessage completedEventResponseBody = await _client.PostAsJsonAsync(
            $"/api/events/{completedEventId}/complete",
            new CompleteWorkRequest(new DateOnly(2028, 6, 8), "点検完了", eventReport));
        Assert.Equal(HttpStatusCode.OK, completedEventResponseBody.StatusCode);

        using var completedHistoryJson = JsonDocument.Parse(await _client.GetStringAsync("/api/history"));
        JsonElement completedEventHistory = completedHistoryJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("title").GetString() == "完了点検");
        Assert.Equal(45, completedEventHistory.GetProperty("report").GetProperty("workMinutes").GetInt32());
    }

    [Fact]
    public async Task CustomerIdentity_RestrictsReadsToLinkedCustomerAndDeniesWrites()
    {
        HttpResponseMessage customerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Scoped Customer", "scoped@example.test", "000"));
        using var customerJson = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
        var customerId = customerJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage bikeResponse = await _client.PostAsJsonAsync("/api/bikes",
            new Bike(0, customerId, "Test", "Scoped", 2026, $"SCOPE-{Guid.NewGuid():N}"));
        using var bikeJson = JsonDocument.Parse(await bikeResponse.Content.ReadAsStringAsync());
        var bikeId = bikeJson.RootElement.GetProperty("id").GetInt32();

        HttpResponseMessage contractResponse = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractRequest(bikeId, 3, new DateOnly(2027, 11, 15), new DateOnly(2027, 12, 1), 1, 1));
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        HttpResponseMessage workResponse = await _client.PostAsJsonAsync("/api/maintenance",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 4, 10), null, "Scoped maintenance", "Test details", 1, 1));
        Assert.Equal(HttpStatusCode.Created, workResponse.StatusCode);
        using var workJson = JsonDocument.Parse(await workResponse.Content.ReadAsStringAsync());
        var workId = workJson.RootElement.GetProperty("id").GetInt32();
        var customerReport = new CompleteWorkReportRequest(
            60,
            [new WorkChecklistItem("ブレーキ", "良好", "動作確認済み")],
            [new WorkReportPart("ブレーキパッド", 1, "個")]);
        HttpResponseMessage completionResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/complete",
            new CompleteWorkRequest(new DateOnly(2028, 4, 10), "scoped-test", customerReport));
        Assert.Equal(HttpStatusCode.OK, completionResponse.StatusCode);
        HttpResponseMessage eventResponse = await _client.PostAsJsonAsync("/api/events", new CreateEventRequest(
            bikeId, "Scoped event", new DateOnly(2028, 5, 1), new DateOnly(2028, 4, 28), null, "", 1, 1));
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        using var eventJson = JsonDocument.Parse(await eventResponse.Content.ReadAsStringAsync());
        Assert.Equal("案内待ち", eventJson.RootElement.GetProperty("status").GetString());

        using HttpResponseMessage linkResponse = await _client.PutAsJsonAsync($"/api/customers/{customerId}/identity",
            new CustomerIdentityRequest("external-tenant", "customer-object"));
        Assert.Equal(HttpStatusCode.NoContent, linkResponse.StatusCode);

        using HttpResponseMessage updateCustomerResponse = await _client.PutAsJsonAsync($"/api/customers/{customerId}",
            new Customer(customerId, "Scoped Customer", "updated@example.test", "123-456"));
        Assert.Equal(HttpStatusCode.OK, updateCustomerResponse.StatusCode);

        HttpResponseMessage duplicateCustomerResponse = await _client.PostAsJsonAsync("/api/customers",
            new Customer(0, "Duplicate Link", "duplicate@example.test", "000"));
        using var duplicateCustomerJson = JsonDocument.Parse(await duplicateCustomerResponse.Content.ReadAsStringAsync());
        var duplicateCustomerId = duplicateCustomerJson.RootElement.GetProperty("id").GetInt32();
        using HttpResponseMessage duplicateLinkResponse = await _client.PutAsJsonAsync($"/api/customers/{duplicateCustomerId}/identity",
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
        using var customerHistoryJson = JsonDocument.Parse(await _client.GetStringAsync("/api/history"));
        JsonElement customerHistory = customerHistoryJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("bikeId").GetInt32() == bikeId);
        Assert.Equal("良好", customerHistory.GetProperty("report").GetProperty("checklist")[0]
            .GetProperty("result").GetString());
        Assert.Equal("ブレーキパッド", customerHistory.GetProperty("report").GetProperty("parts")[0]
            .GetProperty("name").GetString());
        using HttpResponseMessage deniedShops = await _client.GetAsync("/api/shops");
        using HttpResponseMessage deniedStaff = await _client.GetAsync("/api/staff");
        Assert.Equal(HttpStatusCode.Forbidden, deniedShops.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedStaff.StatusCode);

        using HttpResponseMessage foreignCustomer = await _client.GetAsync("/api/customers/1");
        using HttpResponseMessage foreignBike = await _client.GetAsync("/api/bikes/1");
        Assert.Equal(HttpStatusCode.NotFound, foreignCustomer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignBike.StatusCode);

        using HttpResponseMessage deniedWrite = await _client.PostAsJsonAsync("/api/maintenance",
            new CreateWorkRequest(bikeId, new DateOnly(2028, 4, 11), null, "Denied", "", 1, 1));
        Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);
    }

    private async Task AssertOwnedCollectionAsync(string path, string bikeIdProperty, int expectedId)
    {
        using var json = JsonDocument.Parse(await _client.GetStringAsync(path));
        JsonElement[] items = json.RootElement.EnumerateArray().ToArray();
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
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "StaffAdmin";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "integration-user"),
            new("name", "Integration Test"),
            new("roles", role)
        };
        if (Request.Headers.TryGetValue("X-Test-TenantId", out StringValues tenantId))
        {
            claims.Add(new("tid", tenantId.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-ObjectId", out StringValues objectId))
        {
            claims.Add(new("oid", objectId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName, "name", "roles");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
