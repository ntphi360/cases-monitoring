using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HoSoMonitoring.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<NotificationPageResult>> GetNotifications(
        [FromQuery] int? userId,
        [FromQuery] bool? isRead,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        pageIndex = Math.Max(pageIndex, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await _unitOfWork.Notifications.GetPagingAsync(
            ResolveUserId(userId),
            isRead,
            pageIndex,
            pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NotificationDto>> GetNotification(int id)
    {
        var notification = await _unitOfWork.Notifications.GetDetailAsync(id);
        if (notification == null)
        {
            return NotFound();
        }
        if (!CanAccess(notification.UserId)) return Forbid();

        return Ok(new NotificationDto
        {
            Id = notification.Id,
            CaseId = notification.CaseId,
            ExternalCaseCode = notification.Case?.ExternalCaseCode,
            UserId = notification.UserId,
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        });
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
        if (notification == null)
        {
            return NotFound();
        }
        if (!CanAccess(notification.UserId)) return Forbid();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _unitOfWork.CompleteAsync();
        }

        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead([FromQuery] int? userId)
    {
        await _unitOfWork.Notifications.MarkAllReadAsync(ResolveUserId(userId));
        return NoContent();
    }

    private int ResolveUserId(int? requestedUserId)
    {
        if (User.IsInRole(Roles.Admin) && requestedUserId.HasValue) return requestedUserId.Value;
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private bool CanAccess(int ownerUserId) =>
        User.IsInRole(Roles.Admin)
        || ownerUserId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
