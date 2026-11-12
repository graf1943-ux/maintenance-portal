using System.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Services;

public sealed record ServiceResult<T>(T? Value, string? Error, bool IsNotFound = false)
{
    public static ServiceResult<T> Success(T value) => new(value, null);
    public static ServiceResult<T> Invalid(string message) => new(default, message);
    public static ServiceResult<T> NotFound() => new(default, null, true);
}

public sealed class RoadbikeCarePersistenceService(RoadbikeCareDbContext db)
{
    public async Task<ServiceResult<Customer>> CreateCustomerAsync(Customer input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return ServiceResult<Customer>.Invalid("顧客名は必須です。");
        }

        Customer item = input with { Id = await NewIdAsync(db.Customers, x => x.Id, cancellationToken) };
        db.Customers.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<Customer>.Success(item);
    }

    public async Task<ServiceResult<Bike>> CreateBikeAsync(Bike input, CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == input.CustomerId, cancellationToken))
        {
            return ServiceResult<Bike>.Invalid("指定された顧客が存在しません。");
        }

        Bike item = input with { Id = await NewIdAsync(db.Bikes, x => x.Id, cancellationToken), ActiveContractId = null };
        db.Bikes.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<Bike>.Success(await BikeQuery().SingleAsync(x => x.Id == item.Id, cancellationToken));
    }

    public async Task<ServiceResult<StaffMember>> CreateStaffAsync(StaffMember input, CancellationToken cancellationToken)
    {
        if (!await db.Shops.AnyAsync(x => x.Id == input.ShopId, cancellationToken))
        {
            return ServiceResult<StaffMember>.Invalid("指定された店舗が存在しません。");
        }

        StaffMember item = input with { Id = await NewIdAsync(db.Staff, x => x.Id, cancellationToken) };
        db.Staff.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<StaffMember>.Success(item);
    }

    public async Task<ServiceResult<Plan>> CreatePlanAsync(Plan input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.OverhaulCount < 0 || input.MaintenanceCount < 0 || input.InspectionCount < 0)
        {
            return ServiceResult<Plan>.Invalid("プラン名と0以上の回数を指定してください。");
        }

        if (await db.Plans.AnyAsync(x => x.Name.ToLower() == input.Name.ToLower(), cancellationToken))
        {
            return ServiceResult<Plan>.Invalid("同じ名前のプランが存在します。");
        }

        Plan item = input with { Id = await NewIdAsync(db.Plans, x => x.Id, cancellationToken) };
        db.Plans.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<Plan>.Success(item);
    }

    public async Task<ServiceResult<Shop>> CreateShopAsync(Shop input, CancellationToken cancellationToken)
    {
        Shop item = input with { Id = await NewIdAsync(db.Shops, x => x.Id, cancellationToken) };
        db.Shops.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<Shop>.Success(item);
    }

    public async Task<ServiceResult<Contract>> CreateContractAsync(CreateContractRequest input, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        Bike? bike = await db.Bikes.SingleOrDefaultAsync(x => x.Id == input.BikeId, cancellationToken);
        if (bike is null ||
            !await db.Plans.AnyAsync(x => x.Id == input.PlanId, cancellationToken) ||
            !await db.Shops.AnyAsync(x => x.Id == input.ShopId, cancellationToken) ||
            !await db.Staff.AnyAsync(x => x.Id == input.ReceptionistId && x.ShopId == input.ShopId, cancellationToken))
        {
            return ServiceResult<Contract>.Invalid("自転車・プラン・店舗・受付担当者の指定を確認してください。");
        }

        if (input.StartDate.Month != 12 || input.StartDate.Day != 1)
        {
            return ServiceResult<Contract>.Invalid("年間契約の開始日は12月1日です。");
        }

        if (input.ApplicationDate.Year != input.StartDate.Year || input.ApplicationDate > new DateOnly(input.StartDate.Year, 12, 31))
        {
            return ServiceResult<Contract>.Invalid("契約受付日は契約開始年の12月31日までです。");
        }

        if (await db.Contracts.AnyAsync(x => x.BikeId == input.BikeId && x.IsActive, cancellationToken))
        {
            return ServiceResult<Contract>.Invalid("この自転車には有効な年間契約がすでにあります。");
        }

        var contract = new Contract(await NewIdAsync(db.Contracts, x => x.Id, cancellationToken), input.BikeId, input.PlanId, input.ApplicationDate, input.StartDate,
            new DateOnly(input.StartDate.Year + 1, 11, 30), input.ShopId, input.ReceptionistId);
        db.Contracts.Add(contract);
        await db.SaveChangesAsync(cancellationToken);
        db.Entry(bike).Property(x => x.ActiveContractId).CurrentValue = contract.Id;
        await db.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<Contract>.Success(await ContractQuery().SingleAsync(x => x.Id == contract.Id, cancellationToken));
    }

    public async Task<ServiceResult<WorkItem>> CreateWorkAsync(string kind, CreateWorkRequest input, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        if (!await db.Bikes.AnyAsync(x => x.Id == input.BikeId, cancellationToken))
        {
            return ServiceResult<WorkItem>.Invalid("指定された自転車が存在しません。");
        }

        if (!await HasShopReceptionistAsync(input.ShopId, input.ReceptionistId, cancellationToken))
        {
            return ServiceResult<WorkItem>.Invalid("店舗または受付担当者の指定を確認してください。");
        }

        if (input.EndDate is not null && input.EndDate < input.StartDate)
        {
            return ServiceResult<WorkItem>.Invalid("作業完了予定日は開始日以降にしてください。");
        }

        if (kind == "overhaul" && !await IsOverhaulWindowAsync(input.BikeId, input.StartDate, cancellationToken))
        {
            return ServiceResult<WorkItem>.Invalid("オーバーホールの実施期間は契約開始年11月1日から翌年2月末までです。");
        }

        if (!await HasScheduleCapacityAsync(input.BikeId, kind, cancellationToken))
        {
            return ServiceResult<WorkItem>.Invalid("契約上限回数に達しています。既存の予定または実施履歴を確認してください。");
        }

        var item = new WorkItem(await NewIdAsync(db.WorkItems, x => x.Id, cancellationToken), input.BikeId, kind, input.StartDate, input.EndDate, "予定",
            input.Title, input.Detail, input.ShopId, input.ReceptionistId);
        db.WorkItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<WorkItem>.Success(await WorkQuery().SingleAsync(x => x.Id == item.Id, cancellationToken));
    }

    public async Task<ServiceResult<WorkItem>> UpdateWorkAsync(int id, UpdateWorkRequest input, CancellationToken cancellationToken)
    {
        WorkItem? item = await db.WorkItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<WorkItem>.NotFound();
        }

        if (input.EndDate is not null && input.EndDate < input.StartDate)
        {
            return ServiceResult<WorkItem>.Invalid("完了予定日は開始日以降にしてください。");
        }

        if (item.Status == "完了")
        {
            return ServiceResult<WorkItem>.Invalid("完了済みの作業は変更できません。");
        }

        WorkItem updated = item with
        {
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Status = input.Status,
            Title = input.Title,
            Detail = input.Detail,
            ReceptionistId = input.ReceptionistId
        };
        db.Entry(item).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<WorkItem>.Success(await WorkQuery().SingleAsync(x => x.Id == id, cancellationToken));
    }

    public async Task<ServiceResult<WorkItem>> CompleteWorkAsync(int id, CompleteWorkRequest input, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        WorkItem? item = await db.WorkItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<WorkItem>.NotFound();
        }

        if (item.Status == "完了")
        {
            return ServiceResult<WorkItem>.Invalid("この作業はすでに完了しています。");
        }

        Contract? contract = await GetContractWithCapacityAsync(item.BikeId, item.Kind, cancellationToken);
        if (contract is null)
        {
            return ServiceResult<WorkItem>.Invalid("契約の残回数がありません。作業完了として登録できません。");
        }

        WorkItem updated = item with { Status = "完了", CompletedDate = input.CompletedDate, Memo = input.Memo };
        db.Entry(item).CurrentValues.SetValues(updated);
        db.Entry(contract).CurrentValues.SetValues(Consume(contract, item.Kind));
        db.History.Add(new(await NewIdAsync(db.History, x => x.Id, cancellationToken), item.BikeId, input.CompletedDate, item.StartDate,
            item.Kind == "overhaul" ? "オーバーホール" : "メンテナンス", item.Title, item.Detail, input.Memo));
        await db.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<WorkItem>.Success(await WorkQuery().SingleAsync(x => x.Id == id, cancellationToken));
    }

    public async Task<ServiceResult<EventCheck>> CreateEventAsync(CreateEventRequest input, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        if (!await db.Bikes.AnyAsync(x => x.Id == input.BikeId, cancellationToken))
        {
            return ServiceResult<EventCheck>.Invalid("指定された自転車が存在しません。");
        }

        if (!await HasShopReceptionistAsync(input.ShopId, input.ReceptionistId, cancellationToken))
        {
            return ServiceResult<EventCheck>.Invalid("店舗または受付担当者の指定を確認してください。");
        }

        if (input.InspectionEndDate is not null && input.InspectionEndDate < input.InspectionStartDate)
        {
            return ServiceResult<EventCheck>.Invalid("点検完了予定日は開始日以降にしてください。");
        }

        if (!await HasScheduleCapacityAsync(input.BikeId, "inspection", cancellationToken))
        {
            return ServiceResult<EventCheck>.Invalid("点検サポートの契約上限回数に達しています。既存の予定または実施履歴を確認してください。");
        }

        var item = new EventCheck(await NewIdAsync(db.Events, x => x.Id, cancellationToken), input.BikeId, input.Name, input.EventDate, input.InspectionStartDate,
            input.InspectionEndDate, "予定", input.Memo, input.ShopId, input.ReceptionistId);
        db.Events.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<EventCheck>.Success(await EventQuery().SingleAsync(x => x.Id == item.Id, cancellationToken));
    }

    public async Task<ServiceResult<EventCheck>> UpdateEventAsync(int id, EventCheck input, CancellationToken cancellationToken)
    {
        EventCheck? item = await db.Events.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<EventCheck>.NotFound();
        }

        db.Entry(item).CurrentValues.SetValues(input with { Id = id });
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<EventCheck>.Success(await EventQuery().SingleAsync(x => x.Id == id, cancellationToken));
    }

    public async Task<ServiceResult<EventCheck>> CompleteEventAsync(int id, CompleteWorkRequest input, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        EventCheck? item = await db.Events.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<EventCheck>.NotFound();
        }

        if (item.Status == "完了")
        {
            return ServiceResult<EventCheck>.Invalid("この点検はすでに完了しています。");
        }

        Contract? contract = await GetContractWithCapacityAsync(item.BikeId, "inspection", cancellationToken);
        if (contract is null)
        {
            return ServiceResult<EventCheck>.Invalid("点検サポートの契約残回数がありません。");
        }

        EventCheck updated = item with { Status = "完了", CompletedDate = input.CompletedDate, Memo = input.Memo ?? item.Memo };
        db.Entry(item).CurrentValues.SetValues(updated);
        db.Entry(contract).CurrentValues.SetValues(Consume(contract, "inspection"));
        db.History.Add(new(await NewIdAsync(db.History, x => x.Id, cancellationToken), item.BikeId, input.CompletedDate, item.InspectionStartDate,
            "点検サポート", item.Name, item.Memo, input.Memo));
        await db.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<EventCheck>.Success(await EventQuery().SingleAsync(x => x.Id == id, cancellationToken));
    }

    private IQueryable<Bike> BikeQuery() => db.Bikes.AsNoTracking()
        .Include(x => x.CustomerNavigation)
        .Include(x => x.ActiveContractNavigation)
        .ThenInclude(x => x!.PlanNavigation);

    private IQueryable<Contract> ContractQuery() => db.Contracts.AsNoTracking()
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.CustomerNavigation)
        .Include(x => x.PlanNavigation)
        .Include(x => x.ShopNavigation)
        .Include(x => x.ReceptionistNavigation);

    private IQueryable<WorkItem> WorkQuery() => db.WorkItems.AsNoTracking()
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.CustomerNavigation)
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.ActiveContractNavigation).ThenInclude(x => x!.PlanNavigation)
        .Include(x => x.ShopNavigation)
        .Include(x => x.ReceptionistNavigation);

    private IQueryable<EventCheck> EventQuery() => db.Events.AsNoTracking()
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.CustomerNavigation)
        .Include(x => x.BikeNavigation).ThenInclude(x => x!.ActiveContractNavigation).ThenInclude(x => x!.PlanNavigation);

    private async Task<bool> HasShopReceptionistAsync(int shopId, int receptionistId, CancellationToken cancellationToken) =>
        await db.Shops.AnyAsync(x => x.Id == shopId, cancellationToken) &&
        await db.Staff.AnyAsync(x => x.Id == receptionistId && x.ShopId == shopId, cancellationToken);

    private async Task<bool> IsOverhaulWindowAsync(int bikeId, DateOnly startDate, CancellationToken cancellationToken)
    {
        Contract? contract = await db.Contracts.FirstOrDefaultAsync(x => x.BikeId == bikeId && x.IsActive, cancellationToken);
        if (contract is null)
        {
            return false;
        }

        var start = new DateOnly(contract.StartDate.Year, 11, 1);
        var end = new DateOnly(contract.StartDate.Year + 1, 2, DateTime.DaysInMonth(contract.StartDate.Year + 1, 2));
        return startDate >= start && startDate <= end;
    }

    private async Task<bool> HasScheduleCapacityAsync(int bikeId, string kind, CancellationToken cancellationToken)
    {
        Contract? contract = await db.Contracts.FirstOrDefaultAsync(x => x.BikeId == bikeId && x.IsActive, cancellationToken);
        if (contract is null)
        {
            return false;
        }

        Plan? plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == contract.PlanId, cancellationToken);
        if (plan is null)
        {
            return false;
        }

        var limit = kind switch
        {
            "overhaul" => plan.OverhaulCount,
            "maintenance" => plan.MaintenanceCount,
            "inspection" => plan.InspectionCount,
            _ => 0
        };
        var reserved = kind == "inspection"
            ? await db.Events.CountAsync(x => x.BikeId == bikeId && x.Status != "完了" && x.Status != "キャンセル", cancellationToken)
            : await db.WorkItems.CountAsync(x => x.BikeId == bikeId && x.Kind == kind && x.Status != "完了" && x.Status != "キャンセル", cancellationToken);
        var used = kind switch
        {
            "overhaul" => contract.OverhaulUsed,
            "maintenance" => contract.MaintenanceUsed,
            "inspection" => contract.InspectionUsed,
            _ => limit
        };
        return used + reserved < limit;
    }

    private async Task<Contract?> GetContractWithCapacityAsync(int bikeId, string kind, CancellationToken cancellationToken)
    {
        Contract? contract = await db.Contracts.FirstOrDefaultAsync(x => x.BikeId == bikeId && x.IsActive, cancellationToken);
        if (contract is null)
        {
            return null;
        }

        Plan? plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == contract.PlanId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var available = kind switch
        {
            "overhaul" => contract.OverhaulUsed < plan.OverhaulCount,
            "maintenance" => contract.MaintenanceUsed < plan.MaintenanceCount,
            "inspection" => contract.InspectionUsed < plan.InspectionCount,
            _ => false
        };
        return available ? contract : null;
    }

    private static Contract Consume(Contract contract, string kind) => kind switch
    {
        "overhaul" => contract with { OverhaulUsed = contract.OverhaulUsed + 1 },
        "maintenance" => contract with { MaintenanceUsed = contract.MaintenanceUsed + 1 },
        "inspection" => contract with { InspectionUsed = contract.InspectionUsed + 1 },
        _ => contract
    };

    private async Task<int> NewIdAsync<TEntity>(DbSet<TEntity> set, Expression<Func<TEntity, int>> id, CancellationToken cancellationToken)
        where TEntity : class
    {
        if (db.Database.IsRelational())
        {
            return 0;
        }

        return await set.Select(id).DefaultIfEmpty().MaxAsync(cancellationToken) + 1;
    }

    private Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) =>
        db.Database.IsRelational()
            ? BeginRelationalTransactionAsync(cancellationToken)
            : Task.FromResult<IDbContextTransaction?>(null);

    private async Task<IDbContextTransaction?> BeginRelationalTransactionAsync(CancellationToken cancellationToken) =>
        await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    private static Task CommitAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken) =>
        transaction is null ? Task.CompletedTask : transaction.CommitAsync(cancellationToken);
}
