using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Models.Reminder;
using HoSoMonitoring.Core.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HoSoMonitoring.Data.Services;

public class ZaloNotificationService : IZaloNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ZaloOptions _options;

    public ZaloNotificationService(
        HttpClient httpClient,
        ZaloOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ChannelSendResult> SendAsync(
        string recipient,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return Failed("Cán bộ xử lý chưa có thông tin nhận Zalo.");
        }

        if (!_options.IsConfigured)
        {
            return Failed("Zalo chưa được cấu hình.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                _options.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _options.AccessToken);
            request.Content = JsonContent.Create(new
            {
                recipient,
                message
            });

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Failed(
                    $"Gửi Zalo thất bại ({(int)response.StatusCode}).");
            }

            return new ChannelSendResult
            {
                Success = true,
                Message = "Đã gửi Zalo."
            };
        }
        catch (Exception exception)
        {
            return Failed($"Không thể gửi Zalo: {exception.Message}");
        }
    }

    private static ChannelSendResult Failed(string message) => new()
    {
        Success = false,
        Message = message
    };
}
