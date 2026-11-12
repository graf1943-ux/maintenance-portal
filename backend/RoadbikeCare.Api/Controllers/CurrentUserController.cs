using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalUser")]
[Route("api/me")]
public sealed class CurrentUserController(RoadbikeCareDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (IsStaff)
        {
            return Ok(new
            {
                role = "staff",
                isStaffAdmin = User.IsInRole("StaffAdmin"),
                name = User.FindFirst("name")?.Value ?? User.Identity?.Name ?? ""
            });
        }

        var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
        if (customerId is null)
        {
            return Forbid();
        }

        Customer customer = await db.Customers.AsNoTracking().SingleAsync(x => x.Id == customerId, cancellationToken);
        return Ok(new { role = "customer", customerId = customer.Id, name = customer.Name, email = customer.Email });
    }
}
