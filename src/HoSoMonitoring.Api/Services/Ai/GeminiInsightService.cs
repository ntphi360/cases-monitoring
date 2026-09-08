using HoSoMonitoring.Core.Models.AiPrediction;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace HoSoMonitoring.Api.Services.Ai;

public class GeminiInsightService : IAiInsightService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiInsightService> _logger;
    private readonly IConfiguration _configuration;

    public GeminiInsightService(
      HttpClient httpClient,
      IOptions<GeminiOptions> options,
      ILogger<GeminiInsightService> logger,
      IConfiguration configuration)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> GenerateInsightAsync(
        AiPredictionResultDto prediction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        var apiKey = _configuration["GEMINI_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình biến môi trường GEMINI_API_KEY.");
        }

        var prompt = BuildPrompt(prediction);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = prompt
                        }
                    }
                }
            },

            generationConfig = new
            {
                temperature = _options.Temperature,
                maxOutputTokens = _options.MaxOutputTokens
            }
        };

        var requestUrl =
            $"https://generativelanguage.googleapis.com/v1beta/models/" +
            $"{_options.Model}:generateContent";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            requestUrl);

        request.Headers.Add(
            "x-goog-api-key",
            apiKey);

        request.Content = JsonContent.Create(
            requestBody);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseJson =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Gemini API lỗi {StatusCode}: {Response}",
                    response.StatusCode,
                    responseJson);

                throw new InvalidOperationException(
                    $"Gemini API trả về lỗi {(int)response.StatusCode}.");
            }

            return ReadInsight(responseJson);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                "Gemini API phản hồi quá lâu.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Không thể kết nối Gemini API.");

            throw new InvalidOperationException(
                "Không thể kết nối Gemini API.",
                exception);
        }
    }

    private static string BuildPrompt(
        AiPredictionResultDto prediction)
    {
        /*
         * Không tự đoán tên property của AiPredictionResultDto.
         * Serialize toàn bộ kết quả ML hiện có thành JSON,
         * Gemini chỉ được phép diễn giải dữ liệu này.
         */
        var predictionJson = JsonSerializer.Serialize(
            prediction,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

        return $"""
        Bạn là trợ lý phân tích tiến độ hồ sơ hành chính.

        Đây là kết quả từ mô hình Machine Learning:

        {predictionJson}

        Hãy viết nhận xét bằng tiếng Việt tự nhiên cho cán bộ xử lý hồ sơ.

        Yêu cầu bắt buộc:
        - Chỉ sử dụng dữ liệu có trong JSON.
        - Không tự thay đổi kết quả dự đoán.
        - Không tự thay đổi mức độ rủi ro.
        - Không bịa thêm thông tin không có trong dữ liệu.
        - Không nói rằng bạn là AI hay mô hình ngôn ngữ.
        - Không sử dụng markdown.
        - Không dùng tiêu đề.
        - Không liệt kê bullet.
        - Viết từ 2 đến 3 câu ngắn gọn.
        - Diễn giải nguyên nhân dựa trên thời gian dự đoán,
          khoảng bất định P80/P90 và mức rủi ro nếu các trường này tồn tại.
        - Nếu dữ liệu cho thấy nguy cơ trễ hạn cao,
          đưa ra khuyến nghị ưu tiên hoặc theo dõi tiến độ.
        - Nếu nguy cơ thấp,
          giải thích rằng hồ sơ hiện còn khoảng an toàn.
        - Nếu độ bất định lớn,
          nhắc rằng thời gian thực tế có thể dao động.
        - Câu chữ cần tự nhiên và có thể thay đổi giữa các hồ sơ.
        - Không sử dụng các câu mẫu cố định kiểu
          "Hồ sơ có nguy cơ trễ hạn trung bình".
        """;
    }

    private static string ReadInsight(
        string responseJson)
    {
        using var document =
            JsonDocument.Parse(responseJson);

        var root = document.RootElement;

        if (!root.TryGetProperty(
                "candidates",
                out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Gemini không trả về nội dung nhận xét.");
        }

        var firstCandidate = candidates[0];

        if (!firstCandidate.TryGetProperty(
                "content",
                out var content)
            || !content.TryGetProperty(
                "parts",
                out var parts)
            || parts.ValueKind != JsonValueKind.Array
            || parts.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Gemini trả về response không hợp lệ.");
        }

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty(
                    "text",
                    out var textElement))
            {
                var text = textElement.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }
        }

        throw new InvalidOperationException(
            "Gemini không trả về nội dung text.");
    }

    // test 
    public async Task<string> TestAsync(
    CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["GEMINI_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình GEMINI_API_KEY.");
        }

        var requestBody = new
        {
            contents = new[]
            {
            new
            {
                parts = new[]
                {
                    new
                    {
                        text = "Chỉ trả lời đúng câu: Gemini kết nối thành công."
                    }
                }
            }
        }
        };

        var requestUrl =
            $"https://generativelanguage.googleapis.com/v1beta/models/" +
            $"{_options.Model}:generateContent";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            requestUrl);

        request.Headers.Add(
            "x-goog-api-key",
            apiKey);

        request.Content = JsonContent.Create(
            requestBody);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var responseJson =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Gemini API lỗi {(int)response.StatusCode}: {responseJson}");
        }

        return ReadInsight(responseJson);
    }
}