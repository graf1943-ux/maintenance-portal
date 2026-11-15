using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Controllers;

[ApiController]
[Route("api/qr-access")]
public sealed class QrAccessController(RoadbikeCareDbContext db) : ApiControllerBase
{
    private static string NewToken(string prefix) => prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    [HttpPost("grants")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreateGrant([FromBody] CreateQrGrantRequest input, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == input.CustomerId, ct))
        {
            return NotFound();
        }

        DateTime now = DateTime.UtcNow;
        db.QrAccessGrants.RemoveRange(await db.QrAccessGrants.Where(x => x.ExpiresAtUtc <= now).ToListAsync(ct));
        db.QrAccessSessions.RemoveRange(await db.QrAccessSessions.Where(x => x.ExpiresAtUtc <= now).ToListAsync(ct));
        var token = NewToken("qrg_");
        DateTime expires = DateTime.UtcNow.AddMinutes(10);
        db.QrAccessGrants.Add(new QrAccessGrant { TokenHash = Hash(token), CustomerId = input.CustomerId, ExpiresAtUtc = expires });
        await db.SaveChangesAsync(ct);
        return Ok(new { code = token, expiresAtUtc = expires });
    }

    [HttpPost("redeem")]
    [AllowAnonymous]
    public async Task<IActionResult> Redeem([FromBody] RedeemQrGrantRequest input, CancellationToken ct)
    {
        QrAccessGrant? grant = await db.QrAccessGrants.SingleOrDefaultAsync(x => x.TokenHash == Hash(input.Code), ct);
        if (grant is null || grant.RedeemedAtUtc is not null || grant.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "QRコードが無効か、有効期限が切れています。スタッフに再発行を依頼してください。" });
        }

        grant.RedeemedAtUtc = DateTime.UtcNow;
        var sessionToken = NewToken("qrs_");
        DateTime sessionExpires = DateTime.UtcNow.AddMinutes(30);
        db.QrAccessSessions.Add(new QrAccessSession { TokenHash = Hash(sessionToken), CustomerId = grant.CustomerId, ExpiresAtUtc = sessionExpires });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Unauthorized(new { message = "このQRコードはすでに使用されています。" }); }
        Customer customer = await db.Customers.AsNoTracking().SingleAsync(x => x.Id == grant.CustomerId, ct);
        return Ok(new { accessToken = sessionToken, expiresAtUtc = sessionExpires, profile = new { role = "customer", customerId = customer.Id, name = customer.Name, email = customer.Email } });
    }

    [HttpDelete("session")]
    [Authorize(Policy = "PortalUser")]
    public async Task<IActionResult> RevokeSession(CancellationToken ct)
    {
        var identifier = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (identifier?.StartsWith("qr:", StringComparison.Ordinal) == true && int.TryParse(identifier[3..], out var sessionId))
        {
            QrAccessSession? session = await db.QrAccessSessions.FindAsync([sessionId], ct);
            if (session is not null) { db.QrAccessSessions.Remove(session); await db.SaveChangesAsync(ct); }
        }
        return NoContent();
    }
}

public sealed record CreateQrGrantRequest(int CustomerId);
public sealed record RedeemQrGrantRequest(string Code);
