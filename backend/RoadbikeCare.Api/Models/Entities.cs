using System.Text.Json.Serialization;

namespace RoadbikeCare.Api.Models;

public sealed record Customer(int Id, string Name, string Email, string Phone = "")
{
    [JsonIgnore] public string? EntraTenantId { get; init; }
    [JsonIgnore] public string? EntraObjectId { get; init; }
}
public sealed record Bike(int Id, int CustomerId, string Maker, string Model, int Year, string Serial,
    int? ActiveContractId = null, DateOnly? PurchaseDate = null)
{
    [JsonIgnore] public Customer? CustomerNavigation { get; init; }
    [JsonIgnore] public Contract? ActiveContractNavigation { get; init; }
}
public sealed record BikeComponent(int Id, int BikeId, string Category, string Manufacturer, string Model,
    DateOnly InstalledDate, DateOnly? RemovedDate = null)
{
    [JsonIgnore] public Bike? BikeNavigation { get; init; }
}
public sealed record Shop(int Id, string Name, string Address, string Phone);
public sealed record StaffMember(int Id, int ShopId, string Name, string Role)
{
    [JsonIgnore] public Shop? ShopNavigation { get; init; }
}
public sealed record Plan(int Id, string Name, int OverhaulCount, int MaintenanceCount, int InspectionCount);
public sealed record Contract(int Id, int BikeId, int PlanId, DateOnly ApplicationDate, DateOnly StartDate,
    DateOnly EndDate, int ShopId, int ReceptionistId, int OverhaulUsed = 0, int MaintenanceUsed = 0,
    int InspectionUsed = 0, bool IsActive = true)
{
    [JsonIgnore] public Bike? BikeNavigation { get; init; }
    [JsonIgnore] public Plan? PlanNavigation { get; init; }
    [JsonIgnore] public Shop? ShopNavigation { get; init; }
    [JsonIgnore] public StaffMember? ReceptionistNavigation { get; init; }
}

public sealed record WorkItem(int Id, int BikeId, string Kind, DateOnly StartDate, DateOnly? EndDate,
    string Status, string Title, string Detail, int ShopId, int ReceptionistId,
    DateOnly? CompletedDate = null, string? Memo = null, bool IsAdHoc = false)
{
    [JsonIgnore] public Bike? BikeNavigation { get; init; }
    [JsonIgnore] public Shop? ShopNavigation { get; init; }
    [JsonIgnore] public StaffMember? ReceptionistNavigation { get; init; }
}
public sealed record EventCheck(int Id, int BikeId, string Name, DateOnly EventDate,
    DateOnly InspectionStartDate, DateOnly? InspectionEndDate, string Status, string Memo,
    int ShopId, int ReceptionistId, DateOnly? CompletedDate = null, bool IsAdHoc = false)
{
    [JsonIgnore] public Bike? BikeNavigation { get; init; }
}
public sealed record WorkHistory(int Id, int BikeId, DateOnly Date, DateOnly? StartDate,
    string Category, string Title, string Detail, string? Memo = null, bool IsAdHoc = false,
    string? ReportJson = null)
{
    [JsonIgnore] public Bike? BikeNavigation { get; init; }
}

public sealed record CreateContractRequest(int BikeId, int PlanId, DateOnly ApplicationDate,
    DateOnly StartDate, int ShopId, int ReceptionistId);
public sealed record CreateBikeComponentRequest(string Category, string Manufacturer, string Model,
    DateOnly InstalledDate);
public sealed record CreateWorkRequest(int BikeId, DateOnly StartDate, DateOnly? EndDate,
    string Title, string Detail, int ShopId, int ReceptionistId, bool IsAdHoc = false);
public sealed record UpdateWorkRequest(DateOnly StartDate, DateOnly? EndDate, string Status,
    string Title, string Detail, int ReceptionistId, bool? IsAdHoc = null);
public sealed record WorkChecklistItem(string Item, string Result, string? Note);
public sealed record WorkReportPart(string Name, decimal Quantity, string Unit, decimal? UnitPrice = null);
public sealed record CompleteWorkReportRequest(int WorkMinutes, List<WorkChecklistItem>? Checklist,
    List<WorkReportPart>? Parts);
public sealed record WorkReportData(int WorkMinutes, List<WorkChecklistItem> Checklist,
    List<WorkReportPart> Parts, string ShopName, string StaffName);
public sealed record CompleteWorkRequest(DateOnly CompletedDate, string? Memo,
    CompleteWorkReportRequest? Report = null);
public sealed record StopWorkRequest(DateOnly StoppedDate, string? Reason);
public sealed record CustomerIdentityRequest(string TenantId, string ObjectId);
public sealed record CreateEventRequest(int BikeId, string Name, DateOnly EventDate,
    DateOnly InspectionStartDate, DateOnly? InspectionEndDate, string Memo,
    int ShopId, int ReceptionistId, bool IsAdHoc = false);
public sealed record UpdateEventRequest(int BikeId, string Name, DateOnly EventDate,
    DateOnly InspectionStartDate, DateOnly? InspectionEndDate, string Status, string Memo,
    int ShopId, int ReceptionistId, bool? IsAdHoc = null);
