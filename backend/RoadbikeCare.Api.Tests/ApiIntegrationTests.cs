using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadbikeCare.Api.Models;
using Xunit;

namespace RoadbikeCare.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
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
        var workId = workJson.RootElement.GetProperty("id").GetInt32();

        var completeRequest = new CompleteWorkRequest(new DateOnly(2028, 1, 12), "integration-test");
        var completedResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/complete", completeRequest);
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        using var completedJson = JsonDocument.Parse(await completedResponse.Content.ReadAsStringAsync());
        Assert.Equal("完了", completedJson.RootElement.GetProperty("status").GetString());

        using var contractsJson = JsonDocument.Parse(await _client.GetStringAsync("/api/contracts"));
        var savedContract = contractsJson.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("bikeId").GetInt32() == bikeId);
        Assert.Equal(1, savedContract.GetProperty("overhaulUsed").GetInt32());

        using var historyJson = JsonDocument.Parse(await _client.GetStringAsync("/api/history"));
        Assert.Single(historyJson.RootElement.EnumerateArray(),
            item => item.GetProperty("bikeId").GetInt32() == bikeId);

        using var duplicateResponse = await _client.PostAsJsonAsync($"/api/work/{workId}/complete", completeRequest);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
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

        using var linkResponse = await _client.PutAsJsonAsync($"/api/customers/{customerId}/identity",
            new CustomerIdentityRequest("external-tenant", "customer-object"));
        Assert.Equal(HttpStatusCode.NoContent, linkResponse.StatusCode);
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