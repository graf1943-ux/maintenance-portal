using System.Text.Json.Serialization;

namespace RoadbikeCare.Api.Models;

public sealed record Customer(int Id, string Name, string Email, string Phone = "")
{
    [JsonIgnore] public string? EntraTenantId { get; init; }
    [JsonIgnore] public string? EntraObjectId { get; init; }
}
public sealed class QrAccessGrant
{
    public int Id { get; set; }
    public required string TokenHash { get; set; }
    public int CustomerId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RedeemedAtUtc { get; set; }
}
public sealed class QrAccessSession
{
    public int Id { get; set; }
    public required string TokenHash { get; set; }
    public int CustomerId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
public sealed class CustomerContact
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public required string Channel { get; set; }
    public required string Summary { get; set; }
    public DateTimeOffset ContactedAtUtc { get; set; }
    public required string CreatedBy { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public int? FollowUpStaffId { get; set; }
    public required string FollowUpStatus { get; set; }
    public string? FollowUpResult { get; set; }

    [JsonIgnore] public Customer? CustomerNavigation { get; set; }
    [JsonIgnore] public StaffMember? FollowUpStaffNavigation { get; set; }
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

public sealed record ScheduleTimeSlot(DateOnly Date, string StartTime, string EndTime);

public sealed record WorkItem(int Id, int BikeId, string Kind, DateOnly StartDate, DateOnly? EndDate,
    string Status, string Title, string Detail, int ShopId, int ReceptionistId,
    DateOnly? CompletedDate = null, string? Memo = null, bool IsAdHoc = false,
    int? EstimatedWorkMinutes = null, string? ScheduleSlotsJson = null)
{
    [JsonIgnore] public Bike? BikeNavigation { get; init; }
    [JsonIgnore] public Shop? ShopNavigation { get; init; }
    [JsonIgnore] public StaffMember? ReceptionistNavigation { get; init; }
}
public sealed record EventCheck(int Id, int BikeId, string Name, DateOnly EventDate,
    DateOnly InspectionStartDate, DateOnly? InspectionEndDate, string Status, string Memo,
    int ShopId, int ReceptionistId, DateOnly? CompletedDate = null, bool IsAdHoc = false,
    int? EstimatedWorkMinutes = null, string? ScheduleSlotsJson = null)
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
    string Title, string Detail, int ShopId, int ReceptionistId, bool IsAdHoc = false,
    int? EstimatedWorkMinutes = null, List<ScheduleTimeSlot>? ScheduleSlots = null);
public sealed record UpdateWorkRequest(DateOnly StartDate, DateOnly? EndDate, string Status,
    string Title, string Detail, int ReceptionistId, bool? IsAdHoc = null,
    int? EstimatedWorkMinutes = null, List<ScheduleTimeSlot>? ScheduleSlots = null);
public sealed record WorkChecklistItem(string Item, string Result, string? Note);
public sealed record WorkReportPart(string Name, decimal Quantity, string Unit, decimal? UnitPrice = null);
public sealed record CompleteWorkReportRequest(int WorkMinutes, List<WorkChecklistItem>? Checklist,
    List<WorkReportPart>? Parts);
public sealed record WorkReportData(int WorkMinutes, List<WorkChecklistItem> Checklist,
    List<WorkReportPart> Parts, string ShopName, string StaffName, int ShopId = 0, int ReceptionistId = 0);
public sealed record CompleteWorkRequest(DateOnly CompletedDate, string? Memo,
    CompleteWorkReportRequest? Report = null);
public sealed record StopWorkRequest(DateOnly StoppedDate, string? Reason);
public sealed record CustomerIdentityRequest(string TenantId, string ObjectId);
public sealed record CreateCustomerContactRequest(
    string Channel,
    string Summary,
    DateOnly? FollowUpDate,
    int? FollowUpStaffId);
public sealed record UpdateCustomerFollowUpRequest(string FollowUpStatus, string? FollowUpResult);
public sealed record CreateEventRequest(int BikeId, string Name, DateOnly EventDate,
    DateOnly InspectionStartDate, DateOnly? InspectionEndDate, string Memo,
    int ShopId, int ReceptionistId, bool IsAdHoc = false,
    int? EstimatedWorkMinutes = null, List<ScheduleTimeSlot>? ScheduleSlots = null);
public sealed record UpdateEventRequest(int BikeId, string Name, DateOnly EventDate,
    DateOnly InspectionStartDate, DateOnly? InspectionEndDate, string Status, string Memo,
    int ShopId, int ReceptionistId, bool? IsAdHoc = null,
    int? EstimatedWorkMinutes = null, List<ScheduleTimeSlot>? ScheduleSlots = null);
