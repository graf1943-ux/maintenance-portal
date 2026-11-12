using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;
using RoadbikeCare.Api.Services;

namespace RoadbikeCare.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalUser")]
[Route("api")]
public sealed class WorkController(RoadbikeCareDbContext db, RoadbikeCarePersistenceService service) : ApiControllerBase
{
    [HttpGet("overhauls")]
    public async Task<IActionResult> GetOverhauls(CancellationToken cancellationToken)
    {
        IQueryable<WorkItem> items = WorkQuery(db).Where(x => x.Kind == "overhaul");
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            items = items.Where(x => x.BikeNavigation!.CustomerId == customerId);
        }
        return Ok((await items.ToListAsync(cancellationToken)).Select(WorkView));
    }

    [HttpGet("maintenance")]
    public async Task<IActionResult> GetMaintenance(CancellationToken cancellationToken)
    {
        IQueryable<WorkItem> items = WorkQuery(db).Where(x => x.Kind == "maintenance");
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            items = items.Where(x => x.BikeNavigation!.CustomerId == customerId);
        }
        return Ok((await items.ToListAsync(cancellationToken)).Select(WorkView));
    }

    [HttpPost("overhauls")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreateOverhaul(CreateWorkRequest input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreateWorkAsync("overhaul", input, cancellationToken),
            item => Created($"/api/overhauls/{item.Id}", WorkView(item)));

    [HttpPost("maintenance")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreateMaintenance(CreateWorkRequest input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreateWorkAsync("maintenance", input, cancellationToken),
            item => Created($"/api/maintenance/{item.Id}", WorkView(item)));

    [HttpPut("work/{id:int}")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> UpdateWork(int id, UpdateWorkRequest input, CancellationToken cancellationToken) =>
        ToActionResult(await service.UpdateWorkAsync(id, input, cancellationToken), item => Ok(WorkView(item)));

    [HttpPost("work/{id:int}/complete")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CompleteWork(int id, CompleteWorkRequest input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CompleteWorkAsync(id, input, cancellationToken), item => Ok(WorkView(item)));

    [HttpDelete("work/{id:int}")]
    [Authorize(Policy = "Staff")]
    public Task<IActionResult> DeleteWork(int id, CancellationToken cancellationToken) =>
        DeleteEntityAsync(db, db.WorkItems, id, cancellationToken);
}
