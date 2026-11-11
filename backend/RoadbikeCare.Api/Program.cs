using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MockDataStore>();

// 開発時は appsettings.Development.json の localhost を使用します。
// Azure では App Service の Cors__AllowedOrigins__0 などで Origin を指定します。
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    }
}));

var app = builder.Build();
app.UseCors("frontend");

app.MapGet("/", () => Results.Ok(new { name = "Roadbike Care API", version = "0.1.0", docs = "/api" }));
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", storage = "in-memory", timestamp = DateTimeOffset.UtcNow }));
app.MapGet("/api", () => Results.Ok(new
{
    message = "Roadbike Care API mock endpoints",
    resources = new[] { "customers", "bikes", "shops", "staff", "plans", "contracts", "overhauls", "maintenance", "events", "history" }
}));

// Master data
app.MapGet("/api/customers", (MockDataStore db) => Results.Ok(db.Customers));
app.MapGet("/api/customers/{id:int}", (int id, MockDataStore db) => Find(db.Customers, id));
app.MapPost("/api/customers", (Customer input, MockDataStore db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name)) return (IResult)Bad("顧客名は必須です。");
    var item = input with { Id = db.NextId(db.Customers, x => x.Id) };
    db.Customers.Add(item);
    return (IResult)Results.Created($"/api/customers/{item.Id}", item);
});
app.MapPut("/api/customers/{id:int}", (int id, Customer input, MockDataStore db) => Replace(db.Customers, id, input with { Id = id }));
app.MapDelete("/api/customers/{id:int}", (int id, MockDataStore db) => Delete(db.Customers, id));

app.MapGet("/api/bikes", (MockDataStore db) => Results.Ok(db.Bikes.Select(b => BikeView(db, b))));
app.MapGet("/api/bikes/{id:int}", (int id, MockDataStore db) =>
{
    var bike = db.Bikes.FirstOrDefault(x => x.Id == id);
    return bike is null ? (IResult)Results.NotFound() : Results.Ok(BikeView(db, bike));
});
app.MapPost("/api/bikes", (Bike input, MockDataStore db) =>
{
    if (!db.Customers.Any(x => x.Id == input.CustomerId)) return (IResult)Bad("指定された顧客が存在しません。");
    var item = input with { Id = db.NextId(db.Bikes, x => x.Id), ActiveContractId = null };
    db.Bikes.Add(item);
    return (IResult)Results.Created($"/api/bikes/{item.Id}", BikeView(db, item));
});
app.MapPut("/api/bikes/{id:int}", (int id, Bike input, MockDataStore db) =>
{
    if (!db.Customers.Any(x => x.Id == input.CustomerId)) return (IResult)Bad("指定された顧客が存在しません。");
    return Replace(db.Bikes, id, input with { Id = id });
});
app.MapDelete("/api/bikes/{id:int}", (int id, MockDataStore db) => Delete(db.Bikes, id));

app.MapGet("/api/shops", (MockDataStore db) => Results.Ok(db.Shops));
app.MapPost("/api/shops", (Shop input, MockDataStore db) =>
{
    var item = input with { Id = db.NextId(db.Shops, x => x.Id) }; db.Shops.Add(item);
    return (IResult)Results.Created($"/api/shops/{item.Id}", item);
});
app.MapPut("/api/shops/{id:int}", (int id, Shop input, MockDataStore db) => Replace(db.Shops, id, input with { Id = id }));
app.MapDelete("/api/shops/{id:int}", (int id, MockDataStore db) => Delete(db.Shops, id));

app.MapGet("/api/staff", (MockDataStore db) => Results.Ok(db.Staff));
app.MapPost("/api/staff", (StaffMember input, MockDataStore db) =>
{
    if (!db.Shops.Any(x => x.Id == input.ShopId)) return (IResult)Bad("指定された店舗が存在しません。");
    var item = input with { Id = db.NextId(db.Staff, x => x.Id) }; db.Staff.Add(item);
    return (IResult)Results.Created($"/api/staff/{item.Id}", item);
});
app.MapPut("/api/staff/{id:int}", (int id, StaffMember input, MockDataStore db) => Replace(db.Staff, id, input with { Id = id }));
app.MapDelete("/api/staff/{id:int}", (int id, MockDataStore db) => Delete(db.Staff, id));

app.MapGet("/api/plans", (MockDataStore db) => Results.Ok(db.Plans));
app.MapPost("/api/plans", (Plan input, MockDataStore db) =>
{
    if (string.IsNullOrWhiteSpace(input.Name) || input.OverhaulCount < 0 || input.MaintenanceCount < 0 || input.InspectionCount < 0)
        return (IResult)Bad("プラン名と0以上の回数を指定してください。");
    if (db.Plans.Any(x => x.Name.Equals(input.Name, StringComparison.OrdinalIgnoreCase))) return (IResult)Bad("同じ名前のプランが存在します。");
    var item = input with { Id = db.NextId(db.Plans, x => x.Id) }; db.Plans.Add(item);
    return (IResult)Results.Created($"/api/plans/{item.Id}", item);
});
app.MapPut("/api/plans/{id:int}", (int id, Plan input, MockDataStore db) => Replace(db.Plans, id, input with { Id = id }));
app.MapDelete("/api/plans/{id:int}", (int id, MockDataStore db) => Delete(db.Plans, id));

// Annual contracts. Period is always Dec 1 through Nov 30.
app.MapGet("/api/contracts", (MockDataStore db) => Results.Ok(db.Contracts.Select(c => ContractView(db, c))));
app.MapGet("/api/contracts/{id:int}", (int id, MockDataStore db) =>
{
    var item = db.Contracts.FirstOrDefault(x => x.Id == id);
    return item is null ? (IResult)Results.NotFound() : Results.Ok(ContractView(db, item));
});
app.MapPost("/api/contracts", (CreateContractRequest input, MockDataStore db) =>
{
    var bike = db.Bikes.FirstOrDefault(x => x.Id == input.BikeId);
    if (bike is null || !db.Plans.Any(x => x.Id == input.PlanId) || !db.Shops.Any(x => x.Id == input.ShopId) || !db.Staff.Any(x => x.Id == input.ReceptionistId && x.ShopId == input.ShopId))
        return (IResult)Bad("自転車・プラン・店舗・受付担当者の指定を確認してください。");
    if (input.StartDate.Month != 12 || input.StartDate.Day != 1) return (IResult)Bad("年間契約の開始日は12月1日です。");
    if (input.ApplicationDate.Year != input.StartDate.Year || input.ApplicationDate > new DateOnly(input.StartDate.Year, 12, 31)) return (IResult)Bad("契約受付日は契約開始年の12月31日までです。");
    if (db.Contracts.Any(x => x.BikeId == input.BikeId && x.IsActive)) return (IResult)Bad("この自転車には有効な年間契約がすでにあります。");
    var contract = new Contract(db.NextId(db.Contracts, x => x.Id), input.BikeId, input.PlanId,
        input.ApplicationDate, input.StartDate, new DateOnly(input.StartDate.Year + 1, 11, 30), input.ShopId, input.ReceptionistId);
    db.Contracts.Add(contract);
    var index = db.Bikes.FindIndex(x => x.Id == bike.Id); db.Bikes[index] = bike with { ActiveContractId = contract.Id };
    return (IResult)Results.Created($"/api/contracts/{contract.Id}", ContractView(db, contract));
});

// Work schedules. kind is overhaul or maintenance.
app.MapGet("/api/overhauls", (MockDataStore db) => Results.Ok(db.WorkItems.Where(x => x.Kind == "overhaul").Select(x => WorkView(db, x))));
app.MapGet("/api/maintenance", (MockDataStore db) => Results.Ok(db.WorkItems.Where(x => x.Kind == "maintenance").Select(x => WorkView(db, x))));
app.MapPost("/api/overhauls", (CreateWorkRequest input, MockDataStore db) => CreateWork("overhaul", input, db));
app.MapPost("/api/maintenance", (CreateWorkRequest input, MockDataStore db) => CreateWork("maintenance", input, db));
app.MapPut("/api/work/{id:int}", (int id, UpdateWorkRequest input, MockDataStore db) =>
{
    var index = db.WorkItems.FindIndex(x => x.Id == id);
    if (index < 0) return (IResult)Results.NotFound();
    if (input.EndDate is not null && input.EndDate < input.StartDate) return (IResult)Bad("完了予定日は開始日以降にしてください。");
    var old = db.WorkItems[index];
    if (old.Status == "完了") return (IResult)Bad("完了済みの作業は変更できません。");
    var updated = old with
    {
        StartDate = input.StartDate,
        EndDate = input.EndDate,
        Status = input.Status,
        Title = input.Title,
        Detail = input.Detail,
        ReceptionistId = input.ReceptionistId
    };
    db.WorkItems[index] = updated;
    return (IResult)Results.Ok(WorkView(db, updated));
});
app.MapPost("/api/work/{id:int}/complete", (int id, CompleteWorkRequest input, MockDataStore db) => CompleteWork(id, input, db));
app.MapDelete("/api/work/{id:int}", (int id, MockDataStore db) => Delete(db.WorkItems, id));

// Event and inspection support
app.MapGet("/api/events", (MockDataStore db) => Results.Ok(db.Events.Select(x => EventView(db, x))));
app.MapPost("/api/events", (CreateEventRequest input, MockDataStore db) =>
{
    if (!db.Bikes.Any(x => x.Id == input.BikeId)) return (IResult)Bad("指定された自転車が存在しません。");
    if (!db.Shops.Any(x => x.Id == input.ShopId) || !db.Staff.Any(x => x.Id == input.ReceptionistId && x.ShopId == input.ShopId)) return (IResult)Bad("店舗または受付担当者の指定を確認してください。");
    if (input.InspectionEndDate is not null && input.InspectionEndDate < input.InspectionStartDate) return (IResult)Bad("点検完了予定日は開始日以降にしてください。");
    if (!HasScheduleCapacity(db, input.BikeId, "inspection")) return (IResult)Bad("点検サポートの契約上限回数に達しています。既存の予定または実施履歴を確認してください。");
    var item = new EventCheck(db.NextId(db.Events, x => x.Id), input.BikeId, input.Name, input.EventDate,
        input.InspectionStartDate, input.InspectionEndDate, "予定", input.Memo, input.ShopId, input.ReceptionistId);
    db.Events.Add(item);
    return (IResult)Results.Created($"/api/events/{item.Id}", EventView(db, item));
});
app.MapPut("/api/events/{id:int}", (int id, EventCheck input, MockDataStore db) =>
{
    var index = db.Events.FindIndex(x => x.Id == id);
    if (index < 0) return (IResult)Results.NotFound();
    var updated = input with { Id = id }; db.Events[index] = updated;
    return (IResult)Results.Ok(EventView(db, updated));
});
app.MapPost("/api/events/{id:int}/complete", (int id, CompleteWorkRequest input, MockDataStore db) =>
{
    var index = db.Events.FindIndex(x => x.Id == id);
    if (index < 0) return (IResult)Results.NotFound();
    var item = db.Events[index];
    if (item.Status == "完了") return (IResult)Bad("この点検はすでに完了しています。");
    if (!HasUnconsumedCount(db, item.BikeId, "inspection")) return (IResult)Bad("点検サポートの契約残回数がありません。");
    var updated = item with { Status = "完了", CompletedDate = input.CompletedDate, Memo = input.Memo ?? item.Memo };
    db.Events[index] = updated;
    ConsumeContractCount(db, item.BikeId, "inspection");
    db.History.Add(new(db.NextId(db.History, x => x.Id), item.BikeId, input.CompletedDate,
        item.InspectionStartDate, "点検サポート", item.Name, item.Memo, input.Memo));
    return (IResult)Results.Ok(EventView(db, updated));
});

app.MapGet("/api/history", (MockDataStore db) => Results.Ok(db.History.OrderByDescending(x => x.Date).Select(x => HistoryView(db, x))));

app.Run();

IResult CreateWork(string kind, CreateWorkRequest input, MockDataStore db)
{
    if (!db.Bikes.Any(x => x.Id == input.BikeId)) return (IResult)Bad("指定された自転車が存在しません。");
    if (!db.Shops.Any(x => x.Id == input.ShopId) || !db.Staff.Any(x => x.Id == input.ReceptionistId && x.ShopId == input.ShopId)) return (IResult)Bad("店舗または受付担当者の指定を確認してください。");
    if (input.EndDate is not null && input.EndDate < input.StartDate) return (IResult)Bad("作業完了予定日は開始日以降にしてください。");
    if (kind == "overhaul" && !IsOverhaulWindow(db, input.BikeId, input.StartDate)) return (IResult)Bad("オーバーホールの実施期間は契約開始年11月1日から翌年2月末までです。");
    if (!HasScheduleCapacity(db, input.BikeId, kind)) return (IResult)Bad("契約上限回数に達しています。既存の予定または実施履歴を確認してください。");
    var item = new WorkItem(db.NextId(db.WorkItems, x => x.Id), input.BikeId, kind, input.StartDate,
        input.EndDate, "予定", input.Title, input.Detail, input.ShopId, input.ReceptionistId);
    db.WorkItems.Add(item);
    return (IResult)Results.Created($"/api/{(kind == "overhaul" ? "overhauls" : "maintenance")}/{item.Id}", WorkView(db, item));
}

IResult CompleteWork(int id, CompleteWorkRequest input, MockDataStore db)
{
    var index = db.WorkItems.FindIndex(x => x.Id == id);
    if (index < 0) return (IResult)Results.NotFound();
    var item = db.WorkItems[index];
    if (item.Status == "完了") return (IResult)Bad("この作業はすでに完了しています。");
    if (!HasUnconsumedCount(db, item.BikeId, item.Kind)) return (IResult)Bad("契約の残回数がありません。作業完了として登録できません。");
    var updated = item with { Status = "完了", CompletedDate = input.CompletedDate, Memo = input.Memo };
    db.WorkItems[index] = updated;
    ConsumeContractCount(db, item.BikeId, item.Kind);
    db.History.Add(new(db.NextId(db.History, x => x.Id), item.BikeId, input.CompletedDate,
        item.StartDate, item.Kind == "overhaul" ? "オーバーホール" : "メンテナンス", item.Title, item.Detail, input.Memo));
    return (IResult)Results.Ok(WorkView(db, updated));
}

bool IsOverhaulWindow(MockDataStore db, int bikeId, DateOnly startDate)
{
    var contract = db.Contracts.FirstOrDefault(x => x.BikeId == bikeId && x.IsActive);
    if (contract is null) return false;
    var start = new DateOnly(contract.StartDate.Year, 11, 1);
    var end = new DateOnly(contract.StartDate.Year + 1, 2, DateTime.DaysInMonth(contract.StartDate.Year + 1, 2));
    return startDate >= start && startDate <= end;
}

bool HasUnconsumedCount(MockDataStore db, int bikeId, string kind)
{
    var contract = db.Contracts.FirstOrDefault(x => x.BikeId == bikeId && x.IsActive);
    if (contract is null) return false;
    var plan = db.Plans.FirstOrDefault(x => x.Id == contract.PlanId);
    if (plan is null) return false;
    return kind switch
    {
        "overhaul" => contract.OverhaulUsed < plan.OverhaulCount,
        "maintenance" => contract.MaintenanceUsed < plan.MaintenanceCount,
        "inspection" => contract.InspectionUsed < plan.InspectionCount,
        _ => false
    };
}

// 新規予定の登録可否。完了済み回数に加え、キャンセル以外の未完了予定も契約枠を確保します。
bool HasScheduleCapacity(MockDataStore db, int bikeId, string kind)
{
    var contract = db.Contracts.FirstOrDefault(x => x.BikeId == bikeId && x.IsActive);
    if (contract is null) return false;
    var plan = db.Plans.FirstOrDefault(x => x.Id == contract.PlanId);
    if (plan is null) return false;
    var limit = kind switch
    {
        "overhaul" => plan.OverhaulCount,
        "maintenance" => plan.MaintenanceCount,
        "inspection" => plan.InspectionCount,
        _ => 0
    };
    var reserved = kind == "inspection"
        ? db.Events.Count(x => x.BikeId == bikeId && x.Status != "完了" && x.Status != "キャンセル")
        : db.WorkItems.Count(x => x.BikeId == bikeId && x.Kind == kind && x.Status != "完了" && x.Status != "キャンセル");
    var used = kind switch
    {
        "overhaul" => contract.OverhaulUsed,
        "maintenance" => contract.MaintenanceUsed,
        "inspection" => contract.InspectionUsed,
        _ => limit
    };
    return used + reserved < limit;
}

void ConsumeContractCount(MockDataStore db, int bikeId, string kind)
{
    var index = db.Contracts.FindIndex(x => x.BikeId == bikeId && x.IsActive);
    if (index < 0) return;
    var c = db.Contracts[index];
    db.Contracts[index] = kind switch
    {
        "overhaul" => c with { OverhaulUsed = c.OverhaulUsed + 1 },
        "maintenance" => c with { MaintenanceUsed = c.MaintenanceUsed + 1 },
        "inspection" => c with { InspectionUsed = c.InspectionUsed + 1 },
        _ => c
    };
}

object BikeView(MockDataStore db, Bike b)
{
    var contract = db.Contracts.FirstOrDefault(x => x.Id == b.ActiveContractId);
    var plan = contract is null ? null : db.Plans.FirstOrDefault(x => x.Id == contract.PlanId);
    return new
    {
        b.Id,
        b.CustomerId,
        Customer = db.Customers.FirstOrDefault(x => x.Id == b.CustomerId)?.Name ?? "",
        b.Maker,
        b.Model,
        b.Year,
        b.Serial,
        b.ActiveContractId,
        ContractStart = contract?.StartDate,
        ContractEnd = contract?.EndDate,
        OverhaulUsed = contract?.OverhaulUsed ?? 0,
        MaintenanceUsed = contract?.MaintenanceUsed ?? 0,
        InspectionUsed = contract?.InspectionUsed ?? 0,
        Plan = plan,
        ShopId = contract?.ShopId,
        ReceptionistId = contract?.ReceptionistId
    };
}
object ContractView(MockDataStore db, Contract c) => new
{
    c.Id,
    c.BikeId,
    Bike = db.Bikes.FirstOrDefault(x => x.Id == c.BikeId),
    Customer = db.Bikes.FirstOrDefault(x => x.Id == c.BikeId) is { } b ? db.Customers.FirstOrDefault(x => x.Id == b.CustomerId)?.Name : null,
    Plan = db.Plans.FirstOrDefault(x => x.Id == c.PlanId),
    Shop = db.Shops.FirstOrDefault(x => x.Id == c.ShopId),
    Receptionist = db.Staff.FirstOrDefault(x => x.Id == c.ReceptionistId),
    c.PlanId,
    c.ApplicationDate,
    c.StartDate,
    c.EndDate,
    c.ShopId,
    c.ReceptionistId,
    c.OverhaulUsed,
    c.MaintenanceUsed,
    c.InspectionUsed,
    c.IsActive
};
object WorkView(MockDataStore db, WorkItem x) => new
{
    x.Id,
    x.BikeId,
    Bike = BikeView(db, db.Bikes.First(b => b.Id == x.BikeId)),
    x.Kind,
    x.StartDate,
    x.EndDate,
    x.Status,
    x.Title,
    x.Detail,
    x.ShopId,
    x.ReceptionistId,
    Shop = db.Shops.FirstOrDefault(s => s.Id == x.ShopId)?.Name,
    Receptionist = db.Staff.FirstOrDefault(s => s.Id == x.ReceptionistId)?.Name,
    x.CompletedDate,
    x.Memo
};
object EventView(MockDataStore db, EventCheck x) => new
{
    x.Id,
    x.BikeId,
    Bike = BikeView(db, db.Bikes.First(b => b.Id == x.BikeId)),
    x.Name,
    x.EventDate,
    x.InspectionStartDate,
    x.InspectionEndDate,
    x.Status,
    x.Memo,
    x.ShopId,
    x.ReceptionistId,
    x.CompletedDate
};
object HistoryView(MockDataStore db, WorkHistory x) => new
{
    x.Id,
    x.BikeId,
    Bike = BikeView(db, db.Bikes.First(b => b.Id == x.BikeId)),
    x.Date,
    x.StartDate,
    x.Category,
    x.Title,
    x.Detail,
    x.Memo
};

static IResult Find<T>(IEnumerable<T> items, int id) where T : notnull =>
    items.FirstOrDefault(x => (int)x!.GetType().GetProperty("Id")!.GetValue(x)! == id) is { } item ? Results.Ok(item) : Results.NotFound();
static IResult Replace<T>(List<T> items, int id, T input) where T : notnull
{
    var index = items.FindIndex(x => (int)x!.GetType().GetProperty("Id")!.GetValue(x)! == id);
    if (index < 0) return (IResult)Results.NotFound();
    items[index] = input; return (IResult)Results.Ok(input);
}
static IResult Delete<T>(List<T> items, int id) where T : notnull
{
    var index = items.FindIndex(x => (int)x!.GetType().GetProperty("Id")!.GetValue(x)! == id);
    if (index < 0) return (IResult)Results.NotFound();
    items.RemoveAt(index); return (IResult)Results.NoContent();
}
static IResult Bad(string message) => Results.BadRequest(new { message });
