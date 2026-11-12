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
public sealed class MasterDataController(RoadbikeCareDbContext db, RoadbikeCarePersistenceService service) : ApiControllerBase
{
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        IQueryable<Customer> customers = db.Customers.AsNoTracking();
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            customers = customers.Where(x => x.Id == customerId);
        }
        return Ok(await customers.ToListAsync(cancellationToken));
    }

    [HttpGet("customers/{id:int}")]
    public async Task<IActionResult> GetCustomer(int id, CancellationToken cancellationToken)
    {
        if (!IsStaff && await CurrentCustomerIdAsync(db, cancellationToken) != id)
        {
            return NotFound();
        }

        return await FindEntityAsync(db.Customers, id, cancellationToken);
    }

    [HttpPut("customers/{id:int}/identity")]
    [Authorize(Policy = "StaffAdmin")]
    public async Task<IActionResult> LinkCustomerIdentity(int id, CustomerIdentityRequest input, CancellationToken cancellationToken)
    {
        var tenantId = input.TenantId?.Trim();
        var objectId = input.ObjectId?.Trim();
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(objectId))
        {
            return BadRequest(new { message = "Entra tenant IDとobject IDを指定してください。" });
        }

        Customer? customer = await db.Customers.FindAsync(new object?[] { id }, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        if (await db.Customers.AnyAsync(x => x.Id != id && x.EntraTenantId == tenantId && x.EntraObjectId == objectId, cancellationToken))
        {
            return Conflict(new { message = "このEntraユーザーは別の顧客に紐付いています。" });
        }

        db.Entry(customer).CurrentValues.SetValues(customer with
        {
            EntraTenantId = tenantId,
            EntraObjectId = objectId
        });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("customers")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreateCustomer(Customer input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreateCustomerAsync(input, cancellationToken),
            item => Created($"/api/customers/{item.Id}", item));

    [HttpPut("customers/{id:int}")]
    [Authorize(Policy = "Staff")]
    public Task<IActionResult> UpdateCustomer(int id, Customer input, CancellationToken cancellationToken) =>
        ReplaceEntityAsync(db, db.Customers, id, input with { Id = id }, cancellationToken);

    [HttpDelete("customers/{id:int}")]
    [Authorize(Policy = "Staff")]
    public Task<IActionResult> DeleteCustomer(int id, CancellationToken cancellationToken) =>
        DeleteEntityAsync(db, db.Customers, id, cancellationToken);

    [HttpGet("bikes")]
    public async Task<IActionResult> GetBikes(CancellationToken cancellationToken)
    {
        IQueryable<Bike> bikes = BikeQuery(db);
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            bikes = bikes.Where(x => x.CustomerId == customerId);
        }
        return Ok((await bikes.ToListAsync(cancellationToken)).Select(BikeView));
    }

    [HttpGet("bikes/{id:int}")]
    public async Task<IActionResult> GetBike(int id, CancellationToken cancellationToken)
    {
        IQueryable<Bike> bikes = BikeQuery(db);
        if (!IsStaff)
        {
            var customerId = await CurrentCustomerIdAsync(db, cancellationToken);
            if (customerId is null)
            {
                return Forbid();
            }

            bikes = bikes.Where(x => x.CustomerId == customerId);
        }
        Bike? bike = await bikes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return bike is null ? NotFound() : Ok(BikeView(bike));
    }

    [HttpPost("bikes")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreateBike(Bike input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreateBikeAsync(input, cancellationToken),
            item => Created($"/api/bikes/{item.Id}", BikeView(item)));

    [HttpPut("bikes/{id:int}")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> UpdateBike(int id, Bike input, CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == input.CustomerId, cancellationToken))
        {
            return BadRequest(new { message = "指定された顧客が存在しません。" });
        }

        return await ReplaceEntityAsync(db, db.Bikes, id, input with { Id = id }, cancellationToken);
    }

    [HttpDelete("bikes/{id:int}")]
    [Authorize(Policy = "Staff")]
    public Task<IActionResult> DeleteBike(int id, CancellationToken cancellationToken) =>
        DeleteEntityAsync(db, db.Bikes, id, cancellationToken);

    [HttpGet("shops")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> GetShops(CancellationToken cancellationToken) =>
        Ok(await db.Shops.AsNoTracking().ToListAsync(cancellationToken));

    [HttpPost("shops")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreateShop(Shop input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreateShopAsync(input, cancellationToken),
            item => Created($"/api/shops/{item.Id}", item));

    [HttpPut("shops/{id:int}")]
    [Authorize(Policy = "Staff")]
    public Task<IActionResult> UpdateShop(int id, Shop input, CancellationToken cancellationToken) =>
        ReplaceEntityAsync(db, db.Shops, id, input with { Id = id }, cancellationToken);

    [HttpDelete("shops/{id:int}")]
    [Authorize(Policy = "Staff")]
    public Task<IActionResult> DeleteShop(int id, CancellationToken cancellationToken) =>
        DeleteEntityAsync(db, db.Shops, id, cancellationToken);

    [HttpGet("staff")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> GetStaff(CancellationToken cancellationToken) =>
        Ok(await db.Staff.AsNoTracking().ToListAsync(cancellationToken));

    [HttpPost("staff")]
    [Authorize(Policy = "StaffAdmin")]
    public async Task<IActionResult> CreateStaff(StaffMember input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreateStaffAsync(input, cancellationToken),
            item => Created($"/api/staff/{item.Id}", item));

    [HttpPut("staff/{id:int}")]
    [Authorize(Policy = "StaffAdmin")]
    public Task<IActionResult> UpdateStaff(int id, StaffMember input, CancellationToken cancellationToken) =>
        ReplaceEntityAsync(db, db.Staff, id, input with { Id = id }, cancellationToken);

    [HttpDelete("staff/{id:int}")]
    [Authorize(Policy = "StaffAdmin")]
    public Task<IActionResult> DeleteStaff(int id, CancellationToken cancellationToken) =>
        DeleteEntityAsync(db, db.Staff, id, cancellationToken);

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken) =>
        Ok(await db.Plans.AsNoTracking().ToListAsync(cancellationToken));

    [HttpPost("plans")]
    [Authorize(Policy = "Staff")]
    public async Task<IActionResult> CreatePlan(Plan input, CancellationToken cancellationToken) =>
        ToActionResult(await service.CreatePlanAsync(input, cancellationToken),
            item => Created($"/api/plans/{item.Id}", item));

    [HttpPut("plans/{id:int}")]
    [Authorize(Policy = "Staff")]
    public Task<IActionResult> UpdatePlan(int id, Plan input, CancellationToken cancellationToken) =>
        ReplaceEntityAsync(db, db.Plans, id, input with { Id = id }, cancellationToken);

    [HttpDelete("plans/{id:int}")]
    [Authorize(Policy = "Staff")]
    public Task<IActionResult> DeletePlan(int id, CancellationToken cancellationToken) =>
        DeleteEntityAsync(db, db.Plans, id, cancellationToken);
}
