using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Models.Reminder;
using HoSoMonitoring.Core.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HoSoMonitoring.Data.Services;

public class ResendEmailService : IEmailService
{
    private const string EmailsEndpoint = "https://api.resend.com/emails";

    private readonly HttpClient _httpClient;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        HttpClient httpClient,
        ResendOptions options,
        ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
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
            return Failed("Resend chưa được cấu hình.");
        }

        var from = string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromAddress
            : $"{_options.FromName} <{_options.FromAddress}>";
        var payload = new Dictionary<string, object>
        {
            ["from"] = from,
            ["to"] = new[] { to },
            ["subject"] = subject,
            ["text"] = body
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, EmailsEndpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ChannelSendResult
                {
                    Success = true,
                    Message = "Đã gửi email."
                };
            }

            var statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);
            _logger.LogWarning(
                "Gửi email qua Resend thất bại với HTTP {StatusCode}. Response body: {ResponseBody}",
                statusCode,
                responseBody);
            return Failed($"Gửi email thất bại (HTTP {statusCode}).");
        }
        catch (HttpRequestException)
        {
            _logger.LogError("Không thể kết nối tới Resend API.");
            return Failed("Không thể kết nối dịch vụ gửi email.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Resend API không phản hồi trong thời gian cho phép.");
            return Failed("Dịch vụ gửi email không phản hồi.");
        }
    }

    private static ChannelSendResult Failed(string message) => new()
    {
        Success = false,
        Message = message
    };
}
