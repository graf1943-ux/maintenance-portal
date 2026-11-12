using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;
using RoadbikeCare.Api.Services;

namespace RoadbikeCare.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected bool IsStaff => User.IsInRole("Staff") || User.IsInRole("StaffAdmin");

    protected async Task<int?> CurrentCustomerIdAsync(RoadbikeCareDbContext db, CancellationToken cancellationToken)
    {
        var tenantId = User.FindFirst("tid")?.Value;
        var objectId = User.FindFirst("oid")?.Value;
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(objectId))
        {
            return null;
        }

        return await db.Customers.AsNoTracking()
            .Where(x => x.EntraTenantId == tenantId && x.EntraObjectId == objectId)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    protected IActionResult ToActionResult<T>(ServiceResult<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (result.Error is not null)
        {
            return BadRequest(new { message = result.Error });
        }

        return onSuccess(result.Value!);
    }

    protected async Task<IActionResult> FindEntityAsync<TEntity>(DbSet<TEntity> items, int id, CancellationToken cancellationToken)
        where TEntity : class
    {
        TEntity? item = await items.FindAsync(new object?[] { id }, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    protected async Task<IActionResult> ReplaceEntityAsync<TEntity>(RoadbikeCareDbContext db, DbSet<TEntity> items,
        int id, TEntity input, CancellationToken cancellationToken) where TEntity : class
    {
        TEntity? existing = await items.FindAsync(new object?[] { id }, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        db.Entry(existing).CurrentValues.SetValues(input);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Ok(input);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "関連データまたは一意制約を確認してください。" });
        }
    }

    protected async Task<IActionResult> DeleteEntityAsync<TEntity>(RoadbikeCareDbContext db, DbSet<TEntity> items,
        int id, CancellationToken cancellationToken) where TEntity : class
    {
        TEntity? existing = await items.FindAsync(new object?[] { id }, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        items.Remove(existing);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "関連データが存在するため削除できません。" });
        }
    }

    protected static IQueryable<Bike> BikeQuery(RoadbikeCareDbContext db) => db.Bikes.AsNoTracking()
        .Include(x => x.CustomerNavigation)
        .Include(x => x.ActiveContractNavigation).ThenInclude(x => x!.PlanNavigation);

    protected static IQueryable<Contract> ContractQuery(RoadbikeCareDbContext db) => db.Contracts.AsNoTracking()
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.CustomerNavigation)
        .Include(x => x.PlanNavigation)
        .Include(x => x.ShopNavigation)
        .Include(x => x.ReceptionistNavigation);

    protected static IQueryable<WorkItem> WorkQuery(RoadbikeCareDbContext db) => db.WorkItems.AsNoTracking()
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.CustomerNavigation)
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.ActiveContractNavigation).ThenInclude(x => x!.PlanNavigation)
        .Include(x => x.ShopNavigation)
        .Include(x => x.ReceptionistNavigation);

    protected static IQueryable<EventCheck> EventQuery(RoadbikeCareDbContext db) => db.Events.AsNoTracking()
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.CustomerNavigation)
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.ActiveContractNavigation).ThenInclude(x => x!.PlanNavigation);

    protected static IQueryable<WorkHistory> HistoryQuery(RoadbikeCareDbContext db) => db.History.AsNoTracking()
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.CustomerNavigation)
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.ActiveContractNavigation).ThenInclude(x => x!.PlanNavigation);

    protected static object BikeView(Bike bike)
    {
        Contract? contract = bike.ActiveContractNavigation;
        return new
        {
            bike.Id,
            bike.CustomerId,
            Customer = bike.CustomerNavigation?.Name ?? "",
            bike.Maker,
            bike.Model,
            bike.Year,
            bike.Serial,
            bike.ActiveContractId,
            ContractStart = contract?.StartDate,
            ContractEnd = contract?.EndDate,
            OverhaulUsed = contract?.OverhaulUsed ?? 0,
            MaintenanceUsed = contract?.MaintenanceUsed ?? 0,
            InspectionUsed = contract?.InspectionUsed ?? 0,
            Plan = contract?.PlanNavigation,
            ShopId = contract?.ShopId,
            ReceptionistId = contract?.ReceptionistId
        };
    }

    protected static object ContractView(Contract contract) => new
    {
        contract.Id,
        contract.BikeId,
        Bike = contract.BikeNavigation,
        Customer = contract.BikeNavigation?.CustomerNavigation?.Name,
        Plan = contract.PlanNavigation,
        Shop = contract.ShopNavigation,
        Receptionist = contract.ReceptionistNavigation,
        contract.PlanId,
        contract.ApplicationDate,
        contract.StartDate,
        contract.EndDate,
        contract.ShopId,
        contract.ReceptionistId,
        contract.OverhaulUsed,
        contract.MaintenanceUsed,
        contract.InspectionUsed,
        contract.IsActive
    };

    protected static object WorkView(WorkItem item) => new
    {
        item.Id,
        item.BikeId,
        Bike = BikeView(item.BikeNavigation!),
        item.Kind,
        item.StartDate,
        item.EndDate,
        item.Status,
        item.Title,
        item.Detail,
        item.ShopId,
        item.ReceptionistId,
        Shop = item.ShopNavigation?.Name,
        Receptionist = item.ReceptionistNavigation?.Name,
        item.CompletedDate,
        item.Memo
    };

    protected static object EventView(EventCheck item) => new
    {
        item.Id,
        item.BikeId,
        Bike = BikeView(item.BikeNavigation!),
        item.Name,
        item.EventDate,
        item.InspectionStartDate,
        item.InspectionEndDate,
        item.Status,
        item.Memo,
        item.ShopId,
        item.ReceptionistId,
        item.CompletedDate
    };

    protected static object HistoryView(WorkHistory item) => new
    {
        item.Id,
        item.BikeId,
        Bike = BikeView(item.BikeNavigation!),
        item.Date,
        item.StartDate,
        item.Category,
        item.Title,
        item.Detail,
        item.Memo
    };
}
