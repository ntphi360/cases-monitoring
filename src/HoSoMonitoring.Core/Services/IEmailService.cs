using HoSoMonitoring.Core.Models.Reminder;

namespace HoSoMonitoring.Core.Services;

public interface IEmailService
{
    Task<ChannelSendResult> SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
