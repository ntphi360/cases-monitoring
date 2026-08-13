using HoSoMonitoring.Core.Models.Reminder;

namespace HoSoMonitoring.Core.Services;

public interface IZaloNotificationService
{
    Task<ChannelSendResult> SendAsync(
        string recipient,
        string message,
        CancellationToken cancellationToken = default);
}
