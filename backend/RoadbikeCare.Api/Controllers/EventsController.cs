using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;
using RoadbikeCare.Api.Services;

namespace RoadbikeCare.Api.Controllers;

[ApiController]
[Authorize(Policy = "PortalUser")]
[Route("api/events")]
public sealed class EventsController(RoadbikeCareDbContext db, RoadbikeCarePersistenceService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEvents(CancellationToken cancellationToken)
    {
        IQueryable<EventCheck> events = EventQuery(db);
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            events = events.Where(x => x.BikeNavigation!.CustomerId == customerId);
        }
        return Ok((await events.ToListAsync(cancellationToken)).Select(EventView));
    }

    [HttpPost]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreateEvent(CreateEventRequest input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreateEventAsync(input, cancellationToken),
            item => Created($"/api/events/{item.Id}", EventView(item)));

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> UpdateEvent(int id, EventCheck input, CancellationToken cancellationToken) =>
        ToActionResult(await service.UpdateEventAsync(id, input, cancellationToken), item => Ok(EventView(item)));

    [HttpPost("{id:int}/complete")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CompleteEvent(int id, CompleteWorkRequest input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CompleteEventAsync(id, input, cancellationToken), item => Ok(EventView(item)));
}
