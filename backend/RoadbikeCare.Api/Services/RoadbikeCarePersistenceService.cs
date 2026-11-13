using System.Data;
using System.Linq.Expressions;
using System.Text.Json;
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

        Bike item = input with
        {
            Id = await NewIdAsync(db.Bikes, x => x.Id, cancellationToken),
            Serial = string.IsNullOrWhiteSpace(input.Serial) ? string.Empty : input.Serial.Trim(),
            ActiveContractId = null
        };
        db.Bikes.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<Bike>.Success(await BikeQuery().SingleAsync(x => x.Id == item.Id, cancellationToken));
    }

    public Task<ServiceResult<BikeComponent>> AddBikeComponentAsync(
        int bikeId,
        CreateBikeComponentRequest input,
        CancellationToken cancellationToken) =>
        ExecuteWithExecutionStrategyAsync(() => AddBikeComponentCoreAsync(bikeId, input, cancellationToken));

    private async Task<ServiceResult<BikeComponent>> AddBikeComponentCoreAsync(
        int bikeId,
        CreateBikeComponentRequest input,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        if (!await db.Bikes.AnyAsync(x => x.Id == bikeId, cancellationToken))
        {
            return ServiceResult<BikeComponent>.NotFound();
        }

        var category = input.Category.Trim();
        var manufacturer = input.Manufacturer.Trim();
        var model = input.Model.Trim();
        if (string.IsNullOrWhiteSpace(category) ||
            string.IsNullOrWhiteSpace(manufacturer) ||
            string.IsNullOrWhiteSpace(model) ||
            category.Length > 100 ||
            manufacturer.Length > 100 ||
            model.Length > 100 ||
            input.InstalledDate == default)
        {
            return ServiceResult<BikeComponent>.Invalid("区分・メーカー・モデル（各100文字以内）・取付日を確認してください。");
        }

        List<BikeComponent> currentComponents = await db.BikeComponents
            .Where(x => x.BikeId == bikeId && x.RemovedDate == null)
            .ToListAsync(cancellationToken);
        var replacedComponents = currentComponents
            .Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (replacedComponents.Any(x => input.InstalledDate < x.InstalledDate))
        {
            return ServiceResult<BikeComponent>.Invalid("取付日は現在のコンポーネントの取付日以降にしてください。");
        }

        foreach (BikeComponent component in replacedComponents)
        {
            db.Entry(component).CurrentValues.SetValues(component with { RemovedDate = input.InstalledDate });
        }

        BikeComponent added = new(
            await NewIdAsync(db.BikeComponents, x => x.Id, cancellationToken),
            bikeId,
            category,
            manufacturer,
            model,
            input.InstalledDate);
        db.BikeComponents.Add(added);
        await db.SaveChangesAsync(cancellationToken);
        BikeComponent result = await db.BikeComponents.AsNoTracking()
            .SingleAsync(x => x.Id == added.Id, cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<BikeComponent>.Success(result);
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

    public Task<ServiceResult<Contract>> CreateContractAsync(CreateContractRequest input, CancellationToken cancellationToken) =>
        ExecuteWithExecutionStrategyAsync(() => CreateContractCoreAsync(input, cancellationToken));

    private async Task<ServiceResult<Contract>> CreateContractCoreAsync(CreateContractRequest input, CancellationToken cancellationToken)
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

        DateOnly endDate = new(input.StartDate.Year + 1, 11, 30);
        if (await db.Contracts.AnyAsync(
                x => x.BikeId == input.BikeId &&
                     x.StartDate <= endDate &&
                     x.EndDate >= input.StartDate,
                cancellationToken))
        {
            return ServiceResult<Contract>.Invalid("この自転車には契約期間が重複する年間契約があります。");
        }

        List<Contract> activeContracts = await db.Contracts
            .Where(x => x.BikeId == input.BikeId && x.IsActive)
            .ToListAsync(cancellationToken);
        if (activeContracts.Any(x => x.StartDate > input.StartDate || x.EndDate >= input.StartDate))
        {
            return ServiceResult<Contract>.Invalid("この自転車には有効な年間契約がすでにあります。");
        }

        foreach (Contract activeContract in activeContracts)
        {
            db.Entry(activeContract).CurrentValues.SetValues(activeContract with { IsActive = false });
        }

        var contract = new Contract(await NewIdAsync(db.Contracts, x => x.Id, cancellationToken), input.BikeId, input.PlanId, input.ApplicationDate, input.StartDate,
            endDate, input.ShopId, input.ReceptionistId);
        db.Contracts.Add(contract);
        await db.SaveChangesAsync(cancellationToken);
        db.Entry(bike).Property(x => x.ActiveContractId).CurrentValue = contract.Id;
        await db.SaveChangesAsync(cancellationToken);
        Contract created = await ContractQuery().SingleAsync(x => x.Id == contract.Id, cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<Contract>.Success(created);
    }

    public Task<ServiceResult<WorkItem>> CreateWorkAsync(string kind, CreateWorkRequest input, CancellationToken cancellationToken) =>
        ExecuteWithExecutionStrategyAsync(() => CreateWorkCoreAsync(kind, input, cancellationToken));

    private async Task<ServiceResult<WorkItem>> CreateWorkCoreAsync(string kind, CreateWorkRequest input, CancellationToken cancellationToken)
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

        var item = new WorkItem(await NewIdAsync(db.WorkItems, x => x.Id, cancellationToken), input.BikeId, kind, input.StartDate, input.EndDate,
            input.IsAdHoc ? "作業中" : "案内待ち", input.Title, input.Detail, input.ShopId, input.ReceptionistId, IsAdHoc: input.IsAdHoc);
        db.WorkItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        WorkItem created = await WorkQuery().SingleAsync(x => x.Id == item.Id, cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<WorkItem>.Success(created);
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

        if (item.Status is "完了" or "作業中止")
        {
            return ServiceResult<WorkItem>.Invalid("完了または中止済みの作業は変更できません。");
        }

        if (input.Status == "作業中止")
        {
            return ServiceResult<WorkItem>.Invalid("作業中止は専用の中止操作から登録してください。");
        }

        WorkItem updated = item with
        {
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Status = input.Status,
            Title = input.Title,
            Detail = input.Detail,
            ReceptionistId = input.ReceptionistId,
            IsAdHoc = input.IsAdHoc ?? item.IsAdHoc
        };
        db.Entry(item).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<WorkItem>.Success(await WorkQuery().SingleAsync(x => x.Id == id, cancellationToken));
    }

    public Task<ServiceResult<WorkItem>> CompleteWorkAsync(int id, CompleteWorkRequest input, CancellationToken cancellationToken) =>
        ExecuteWithExecutionStrategyAsync(() => CompleteWorkCoreAsync(id, input, cancellationToken));

    private static string? ValidateWorkReport(CompleteWorkReportRequest? report)
    {
        if (report is null)
        {
            return null;
        }

        if (report.WorkMinutes is < 1 or > 1440)
        {
            return "作業時間は1〜1440分で入力してください。";
        }

        if (report.Checklist is null || report.Checklist.Count > 50 ||
            report.Checklist.Any(item => item is null ||
                string.IsNullOrWhiteSpace(item.Item) ||
                item.Item.Length > 100 ||
                item.Result is not ("良好" or "調整" or "交換" or "対象外") ||
                (item.Note?.Length ?? 0) > 500))
        {
            return "点検項目は50件以内で、項目名・結果・メモ（500文字以内）を確認してください。";
        }

        if (report.Parts is null || report.Parts.Count > 50 ||
            report.Parts.Any(part => part is null ||
                string.IsNullOrWhiteSpace(part.Name) ||
                part.Name.Length > 100 ||
                part.Quantity <= 0 ||
                part.Quantity > 100000 ||
                part.UnitPrice is < 0 or > 100000000 ||
                (part.UnitPrice is decimal unitPrice && unitPrice != decimal.Truncate(unitPrice)) ||
                string.IsNullOrWhiteSpace(part.Unit) ||
                part.Unit.Length > 20))
        {
            return "使用部品は50件以内で、部品名・数量・単位を確認してください。";
        }

        return null;
    }

    private async Task<string?> CreateWorkReportJsonAsync(
        CompleteWorkReportRequest? report,
        int shopId,
        int receptionistId,
        CancellationToken cancellationToken)
    {
        if (report is null)
        {
            return null;
        }

        var shopName = await db.Shops
            .Where(item => item.Id == shopId)
            .Select(item => item.Name)
            .SingleAsync(cancellationToken);
        var staffName = await db.Staff
            .Where(item => item.Id == receptionistId)
            .Select(item => item.Name)
            .SingleAsync(cancellationToken);
        WorkReportData savedReport = new(
            report.WorkMinutes,
            report.Checklist!.Select(item => item with
            {
                Item = item.Item.Trim(),
                Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim()
            }).ToList(),
            report.Parts!.Select(part => part with
            {
                Name = part.Name.Trim(),
                Unit = part.Unit.Trim()
            }).ToList(),
            shopName,
            staffName);
        return JsonSerializer.Serialize(savedReport);
    }

    private async Task<ServiceResult<WorkItem>> CompleteWorkCoreAsync(int id, CompleteWorkRequest input, CancellationToken cancellationToken)
    {
        var validationError = ValidateWorkReport(input.Report);
        if (validationError is not null)
        {
            return ServiceResult<WorkItem>.Invalid(validationError);
        }

        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        WorkItem? item = await db.WorkItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<WorkItem>.NotFound();
        }

        if (item.Status is "完了" or "作業中止")
        {
            return ServiceResult<WorkItem>.Invalid("この作業はすでに完了または中止しています。");
        }

        Contract? contract = await GetContractWithCapacityAsync(item.BikeId, item.Kind, cancellationToken);
        if (contract is null)
        {
            return ServiceResult<WorkItem>.Invalid("契約の残回数がありません。作業完了として登録できません。");
        }

        var reportJson = await CreateWorkReportJsonAsync(
            input.Report, item.ShopId, item.ReceptionistId, cancellationToken);
        WorkItem updated = item with { Status = "完了", CompletedDate = input.CompletedDate, Memo = input.Memo };
        db.Entry(item).CurrentValues.SetValues(updated);
        db.Entry(contract).CurrentValues.SetValues(Consume(contract, item.Kind));
        db.History.Add(new(await NewIdAsync(db.History, x => x.Id, cancellationToken), item.BikeId, input.CompletedDate, item.StartDate,
            item.Kind == "overhaul" ? "オーバーホール" : "メンテナンス", item.Title, item.Detail,
            input.Memo, item.IsAdHoc, reportJson));
        await db.SaveChangesAsync(cancellationToken);
        WorkItem completed = await WorkQuery().SingleAsync(x => x.Id == id, cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<WorkItem>.Success(completed);
    }

    public Task<ServiceResult<WorkItem>> StopWorkAsync(int id, StopWorkRequest input, CancellationToken cancellationToken) =>
        ExecuteWithExecutionStrategyAsync(() => StopWorkCoreAsync(id, input, cancellationToken));

    private async Task<ServiceResult<WorkItem>> StopWorkCoreAsync(int id, StopWorkRequest input, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        WorkItem? item = await db.WorkItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<WorkItem>.NotFound();
        }

        if (item.Status != "作業中")
        {
            return ServiceResult<WorkItem>.Invalid("作業中の作業のみ中止できます。");
        }
        if (input.StoppedDate == default)
        {
            return ServiceResult<WorkItem>.Invalid("中止日は必須です。");
        }
        if (input.StoppedDate < item.StartDate)
        {
            return ServiceResult<WorkItem>.Invalid("中止日は作業開始日以降の日付を指定してください。");
        }

        WorkItem stopped = item with { Status = "作業中止", Memo = input.Reason };
        db.Entry(item).CurrentValues.SetValues(stopped);
        db.History.Add(new(await NewIdAsync(db.History, x => x.Id, cancellationToken), item.BikeId, input.StoppedDate, item.StartDate,
            "作業中止", item.Title, item.Detail, input.Reason, item.IsAdHoc));
        await db.SaveChangesAsync(cancellationToken);
        WorkItem result = await WorkQuery().SingleAsync(x => x.Id == id, cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<WorkItem>.Success(result);
    }

    public Task<ServiceResult<EventCheck>> CreateEventAsync(CreateEventRequest input, CancellationToken cancellationToken) =>
        ExecuteWithExecutionStrategyAsync(() => CreateEventCoreAsync(input, cancellationToken));

    private async Task<ServiceResult<EventCheck>> CreateEventCoreAsync(CreateEventRequest input, CancellationToken cancellationToken)
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
            input.InspectionEndDate, input.IsAdHoc ? "作業中" : "案内待ち", input.Memo, input.ShopId, input.ReceptionistId, IsAdHoc: input.IsAdHoc);
        db.Events.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        EventCheck created = await EventQuery().SingleAsync(x => x.Id == item.Id, cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<EventCheck>.Success(created);
    }

    public async Task<ServiceResult<EventCheck>> UpdateEventAsync(int id, UpdateEventRequest input, CancellationToken cancellationToken)
    {
        EventCheck? item = await db.Events.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<EventCheck>.NotFound();
        }

        if (item.Status is "完了" or "作業中止" || input.Status == "作業中止")
        {
            return ServiceResult<EventCheck>.Invalid("完了または中止済みの点検は変更できません。");
        }

        EventCheck updated = item with
        {
            BikeId = input.BikeId,
            Name = input.Name,
            EventDate = input.EventDate,
            InspectionStartDate = input.InspectionStartDate,
            InspectionEndDate = input.InspectionEndDate,
            Status = input.Status,
            Memo = input.Memo,
            ShopId = input.ShopId,
            ReceptionistId = input.ReceptionistId,
            IsAdHoc = input.IsAdHoc ?? item.IsAdHoc
        };
        db.Entry(item).CurrentValues.SetValues(updated);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<EventCheck>.Success(await EventQuery().SingleAsync(x => x.Id == id, cancellationToken));
    }

    public Task<ServiceResult<EventCheck>> CompleteEventAsync(int id, CompleteWorkRequest input, CancellationToken cancellationToken) =>
        ExecuteWithExecutionStrategyAsync(() => CompleteEventCoreAsync(id, input, cancellationToken));

    private async Task<ServiceResult<EventCheck>> CompleteEventCoreAsync(int id, CompleteWorkRequest input, CancellationToken cancellationToken)
    {
        var validationError = ValidateWorkReport(input.Report);
        if (validationError is not null)
        {
            return ServiceResult<EventCheck>.Invalid(validationError);
        }

        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        EventCheck? item = await db.Events.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<EventCheck>.NotFound();
        }

        if (item.Status is "完了" or "作業中止")
        {
            return ServiceResult<EventCheck>.Invalid("この点検はすでに完了または中止しています。");
        }

        Contract? contract = await GetContractWithCapacityAsync(item.BikeId, "inspection", cancellationToken);
        if (contract is null)
        {
            return ServiceResult<EventCheck>.Invalid("点検サポートの契約残回数がありません。");
        }

        var reportJson = await CreateWorkReportJsonAsync(
            input.Report, item.ShopId, item.ReceptionistId, cancellationToken);
        EventCheck updated = item with { Status = "完了", CompletedDate = input.CompletedDate, Memo = input.Memo ?? item.Memo };
        db.Entry(item).CurrentValues.SetValues(updated);
        db.Entry(contract).CurrentValues.SetValues(Consume(contract, "inspection"));
        db.History.Add(new(await NewIdAsync(db.History, x => x.Id, cancellationToken), item.BikeId, input.CompletedDate, item.InspectionStartDate,
            "点検サポート", item.Name, item.Memo, input.Memo, item.IsAdHoc, reportJson));
        await db.SaveChangesAsync(cancellationToken);
        EventCheck completed = await EventQuery().SingleAsync(x => x.Id == id, cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<EventCheck>.Success(completed);
    }

    public Task<ServiceResult<EventCheck>> StopEventAsync(int id, StopWorkRequest input, CancellationToken cancellationToken) =>
        ExecuteWithExecutionStrategyAsync(() => StopEventCoreAsync(id, input, cancellationToken));

    private async Task<ServiceResult<EventCheck>> StopEventCoreAsync(int id, StopWorkRequest input, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = await BeginTransactionAsync(cancellationToken);
        EventCheck? item = await db.Events.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<EventCheck>.NotFound();
        }

        if (item.Status != "作業中")
        {
            return ServiceResult<EventCheck>.Invalid("作業中の点検のみ中止できます。");
        }
        if (input.StoppedDate == default)
        {
            return ServiceResult<EventCheck>.Invalid("中止日は必須です。");
        }
        if (input.StoppedDate < item.InspectionStartDate)
        {
            return ServiceResult<EventCheck>.Invalid("中止日は点検開始日以降の日付を指定してください。");
        }

        EventCheck stopped = item with { Status = "作業中止" };
        db.Entry(item).CurrentValues.SetValues(stopped);
        db.History.Add(new(await NewIdAsync(db.History, x => x.Id, cancellationToken), item.BikeId, input.StoppedDate, item.InspectionStartDate,
            "作業中止", item.Name, item.Memo, input.Reason, item.IsAdHoc));
        await db.SaveChangesAsync(cancellationToken);
        EventCheck result = await EventQuery().SingleAsync(x => x.Id == id, cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return ServiceResult<EventCheck>.Success(result);
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
            ? await db.Events.CountAsync(x => x.BikeId == bikeId && x.Status != "完了" && x.Status != "キャンセル" && x.Status != "作業中止", cancellationToken)
            : await db.WorkItems.CountAsync(x => x.BikeId == bikeId && x.Kind == kind && x.Status != "完了" && x.Status != "キャンセル" && x.Status != "作業中止", cancellationToken);
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

    private Task<T> ExecuteWithExecutionStrategyAsync<T>(Func<Task<T>> operation) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            return await operation();
        });

    private Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) =>
        db.Database.IsRelational()
            ? BeginRelationalTransactionAsync(cancellationToken)
            : Task.FromResult<IDbContextTransaction?>(null);

    private async Task<IDbContextTransaction?> BeginRelationalTransactionAsync(CancellationToken cancellationToken) =>
        await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    private static Task CommitAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken) =>
        transaction is null ? Task.CompletedTask : transaction.CommitAsync(cancellationToken);
}
