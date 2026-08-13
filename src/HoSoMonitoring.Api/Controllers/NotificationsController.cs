using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

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
            userId,
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
        await _unitOfWork.Notifications.MarkAllReadAsync(userId);
        return NoContent();
    }
}
