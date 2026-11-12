using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;

namespace RoadbikeCare.Api.Controllers;

[ApiController]
public sealed class SystemController(RoadbikeCareDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("/")]
    public IActionResult Root() => Ok(new { name = "Roadbike Care API", version = "0.1.0", docs = "/api" });

    [AllowAnonymous]
    [HttpGet("/api")]
    public IActionResult ApiInfo() => Ok(new
    {
        message = "Roadbike Care API endpoints",
        resources = new[] { "customers", "bikes", "shops", "staff", "plans", "contracts", "overhauls", "maintenance", "events", "history" }
    });

    [AllowAnonymous]
    [HttpGet("/api/health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var connected = await db.Database.CanConnectAsync(cancellationToken);
        var storage = db.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer" ? "azure-sql" : "development-in-memory";
        var response = new { status = connected ? "ok" : "unavailable", storage, timestamp = DateTimeOffset.UtcNow };
        return connected ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
