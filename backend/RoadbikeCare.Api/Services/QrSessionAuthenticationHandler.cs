using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Services;

public sealed class QrSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    RoadbikeCareDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer qrs_", StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization["Bearer ".Length..];
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        QrAccessSession? session = await db.QrAccessSessions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.ExpiresAtUtc > DateTime.UtcNow, Context.RequestAborted);
        if (session is null)
        {
            return AuthenticateResult.Fail("一時アクセスの期限が切れています。");
        }

        Claim[] claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, $"qr:{session.Id}"),
            new Claim(ClaimTypes.Name, "一時顧客アクセス"),
            new Claim(ClaimTypes.Role, "Customer"),
            new Claim("customer_id", session.CustomerId.ToString())
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
