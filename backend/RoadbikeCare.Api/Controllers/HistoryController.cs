using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalUser")]
[Route("api/history")]
public sealed class HistoryController(RoadbikeCareDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        IQueryable<WorkHistory> history = HistoryQuery(db);
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            history = history.Where(x => x.BikeNavigation!.CustomerId == customerId);
        }
        return Ok((await history.OrderByDescending(x => x.Date).ToListAsync(cancellationToken)).Select(HistoryView));
    }
}
