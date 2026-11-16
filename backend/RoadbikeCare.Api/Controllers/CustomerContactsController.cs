using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadbikeCare.Api.Data;
using RoadbikeCare.Api.Models;

namespace RoadbikeCare.Api.Controllers;

[ApiController]
[Authorize(Policy = "Staff")]
[Route("api/customers/{customerId:int}/contacts")]
public sealed class CustomerContactsController(RoadbikeCareDbContext db) : ControllerBase
{
    private static readonly string[] Channels = ["電話", "来店", "メール", "その他"];
    private static readonly string[] FollowUpStatuses = ["未対応", "完了", "不要"];

    [HttpGet("/api/customer-contacts/follow-ups")]
    public async Task<IActionResult> GetPendingFollowUps(CancellationToken cancellationToken)
    {
        List<CustomerContact> contacts = await db.CustomerContacts.AsNoTracking()
            .Include(x => x.CustomerNavigation)
            .Include(x => x.FollowUpStaffNavigation)
            .Where(x => x.FollowUpDate.HasValue && x.FollowUpStatus == "未対応")
            .OrderBy(x => x.FollowUpDate)
            .ThenBy(x => x.ContactedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(contacts.Select(ContactView));
    }

    [HttpGet]
    public async Task<IActionResult> GetContacts(int customerId, CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == customerId, cancellationToken))
        {
            return NotFound();
        }

        List<CustomerContact> contacts = await db.CustomerContacts.AsNoTracking()
            .Include(x => x.FollowUpStaffNavigation)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.ContactedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(contacts.Select(ContactView));
    }

    [HttpPost]
    public async Task<IActionResult> CreateContact(
        int customerId,
        CreateCustomerContactRequest input,
        CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == customerId, cancellationToken))
        {
            return NotFound();
        }

        var channel = input.Channel?.Trim();
        var summary = input.Summary?.Trim();
        if (string.IsNullOrWhiteSpace(channel) || !Channels.Contains(channel))
        {
            return BadRequest(new { message = "連絡方法を選択してください。" });
        }

        if (string.IsNullOrWhiteSpace(summary) || summary.Length > 1000)
        {
            return BadRequest(new { message = "内容は1〜1000文字で入力してください。" });
        }

        if (input.FollowUpStaffId.HasValue && !input.FollowUpDate.HasValue)
        {
            return BadRequest(new { message = "担当者を設定する場合はフォローアップ日も指定してください。" });
        }

        if (input.FollowUpStaffId is int staffId &&
            !await db.Staff.AnyAsync(x => x.Id == staffId, cancellationToken))
        {
            return BadRequest(new { message = "指定された担当者が存在しません。" });
        }

        var contact = new CustomerContact
        {
            CustomerId = customerId,
            Channel = channel,
            Summary = summary,
            ContactedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = User.FindFirst("preferred_username")?.Value
                ?? User.Identity?.Name
                ?? User.FindFirst("oid")?.Value
                ?? "スタッフ",
            FollowUpDate = input.FollowUpDate,
            FollowUpStaffId = input.FollowUpStaffId,
            FollowUpStatus = input.FollowUpDate.HasValue ? "未対応" : "不要"
        };

        db.CustomerContacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(contact).Reference(x => x.FollowUpStaffNavigation).LoadAsync(cancellationToken);

        return Created($"/api/customers/{customerId}/contacts/{contact.Id}", ContactView(contact));
    }

    [HttpPut("{contactId:int}/follow-up")]
    public async Task<IActionResult> UpdateFollowUp(
        int customerId,
        int contactId,
        UpdateCustomerFollowUpRequest input,
        CancellationToken cancellationToken)
    {
        if (input.FollowUpStatus is null || !FollowUpStatuses.Contains(input.FollowUpStatus))
        {
            return BadRequest(new { message = "フォローアップの状態が正しくありません。" });
        }

        CustomerContact? contact = await db.CustomerContacts
            .Include(x => x.FollowUpStaffNavigation)
            .SingleOrDefaultAsync(
                x => x.CustomerId == customerId && x.Id == contactId,
                cancellationToken);
        if (contact is null)
        {
            return NotFound();
        }

        if (!contact.FollowUpDate.HasValue)
        {
            return BadRequest(new { message = "フォローアップが設定されていない連絡履歴です。" });
        }

        var result = input.FollowUpResult?.Trim();
        if (result?.Length > 1000)
        {
            return BadRequest(new { message = "対応結果は1000文字以内で入力してください。" });
        }

        contact.FollowUpStatus = input.FollowUpStatus;
        contact.FollowUpResult = string.IsNullOrWhiteSpace(result) ? null : result;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ContactView(contact));
    }

    private static object ContactView(CustomerContact contact) => new
    {
        contact.Id,
        contact.CustomerId,
        CustomerName = contact.CustomerNavigation?.Name,
        contact.Channel,
        contact.Summary,
        contact.ContactedAtUtc,
        contact.CreatedBy,
        contact.FollowUpDate,
        contact.FollowUpStaffId,
        FollowUpStaffName = contact.FollowUpStaffNavigation?.Name,
        contact.FollowUpStatus,
        contact.FollowUpResult
    };
}
