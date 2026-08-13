using HoSoMonitoring.Core.Models.Reminder;
using HoSoMonitoring.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;

    public RemindersController(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    // TODO(Auth): Add [Authorize(Roles = "ADMIN")] when authentication is enabled.
    [HttpPost]
    public async Task<ActionResult<SendReminderResultDto>> SendReminder(
        [FromBody] SendReminderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _reminderService.SendAsync(
                request,
                cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ReminderValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
