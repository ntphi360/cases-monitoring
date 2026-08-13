using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Models.Reminder;
using HoSoMonitoring.Core.Services;
using System.Net;
using System.Net.Mail;

namespace HoSoMonitoring.Data.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;

    public SmtpEmailService(EmailOptions options)
    {
        _options = options;
    }

    public async Task<ChannelSendResult> SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return Failed("Cán bộ xử lý chưa có địa chỉ email.");
        }

        if (!_options.IsConfigured)
        {
            return Failed("Email chưa được cấu hình.");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl
            };
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                client.Credentials = new NetworkCredential(
                    _options.Username,
                    _options.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
            return new ChannelSendResult
            {
                Success = true,
                Message = "Đã gửi email."
            };
        }
        catch (Exception exception)
        {
            return Failed($"Không thể gửi email: {exception.Message}");
        }
    }

    private static ChannelSendResult Failed(string message) => new()
    {
        Success = false,
        Message = message
    };
}
