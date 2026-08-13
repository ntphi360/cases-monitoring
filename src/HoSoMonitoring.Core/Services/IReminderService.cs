using HoSoMonitoring.Core.Models.Reminder;

namespace HoSoMonitoring.Core.Services;

public interface IReminderService
{
    Task<SendReminderResultDto> SendAsync(
        SendReminderRequest request,
        CancellationToken cancellationToken = default);
}
