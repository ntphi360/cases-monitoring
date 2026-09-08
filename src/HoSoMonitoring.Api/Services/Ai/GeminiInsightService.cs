using HoSoMonitoring.Core.Models.AiPrediction;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HoSoMonitoring.Api.Services.Ai;

public class GeminiInsightService : IAiInsightService
{
    private static readonly TimeSpan InsightCacheDuration =
        TimeSpan.FromHours(12);

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiInsightService> _logger;
    private readonly IMemoryCache _memoryCache;

    public GeminiInsightService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        IConfiguration configuration,
        ILogger<GeminiInsightService> logger,
        IMemoryCache memoryCache)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _configuration = configuration;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<string> GenerateInsightAsync(
        AiPredictionResultDto prediction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        var cacheKey = CreateCacheKey(prediction);

        if (_memoryCache.TryGetValue(
                cacheKey,
                out string? cachedInsight)
            && !string.IsNullOrWhiteSpace(cachedInsight))
        {
            _logger.LogInformation(
                "Sử dụng Gemini insight từ cache {CacheKey}.",
                cacheKey);

            return cachedInsight;
        }

        var apiKey = _configuration["GEMINI_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình GEMINI_API_KEY.");
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

        request.Content = JsonContent.Create(requestBody);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseJson =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(
                    "Gemini API trả về HTTP 429 do giới hạn quota cho cache key {CacheKey}.",
                    cacheKey);

                throw new InvalidOperationException(
                    "Gemini API đã vượt giới hạn quota.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Gemini API trả về lỗi {StatusCode}: {Response}",
                    (int)response.StatusCode,
                    responseJson);

                throw new InvalidOperationException(
                    $"Gemini API trả về lỗi {(int)response.StatusCode}.");
            }

            var insight = ReadInsight(responseJson);

            ValidateInsight(insight);

            _memoryCache.Set(
                cacheKey,
                insight,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        InsightCacheDuration
                });

            _logger.LogInformation(
                "Đã lưu Gemini insight vào cache {CacheKey} trong {CacheHours} giờ.",
                cacheKey,
                InsightCacheDuration.TotalHours);

            return insight;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                "Gemini API phản hồi quá lâu.");
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException(
                "Không thể kết nối Gemini API.",
                exception);
        }
    }

    private static string BuildPrompt(
        AiPredictionResultDto prediction)
    {
        var predicted = Math.Max(
            prediction.PredictedProcessingHours,
            0.01);

        var p80Upper = prediction.PredictionUpper80;
        var p90Upper = prediction.PredictionUpper90;

        var p80Spread = Math.Max(
            0,
            p80Upper - prediction.PredictedProcessingHours);

        var p90Spread = Math.Max(
            0,
            p90Upper - prediction.PredictedProcessingHours);

        var p80Multiplier = p80Upper / predicted;
        var p90Multiplier = p90Upper / predicted;

        return $"""
        Bạn là trợ lý phân tích tiến độ hồ sơ hành chính.

        Dữ liệu dự đoán:
        - Thời gian xử lý dự kiến trung tâm: {prediction.PredictedProcessingHours:F2} giờ
        - Khoảng dự đoán P80: {prediction.PredictionLower80:F2} đến {prediction.PredictionUpper80:F2} giờ
        - Khoảng dự đoán P90: {prediction.PredictionLower90:F2} đến {prediction.PredictionUpper90:F2} giờ
        - Biên trên P80 cao hơn dự đoán trung tâm khoảng: {p80Spread:F2} giờ
        - Biên trên P90 cao hơn dự đoán trung tâm khoảng: {p90Spread:F2} giờ
        - Biên trên P80 gấp khoảng: {p80Multiplier:F2} lần dự đoán trung tâm
        - Biên trên P90 gấp khoảng: {p90Multiplier:F2} lần dự đoán trung tâm
        - Thời điểm hoàn tất dự kiến: {prediction.PredictedCompletionTime}
        - Thời điểm hoàn tất theo biên P90: {prediction.PredictedCompletionP90}
        - Mức rủi ro trễ hạn đã được hệ thống xác định: {prediction.DeadlineRisk}

        Nhiệm vụ:
        Viết nhận xét bằng tiếng Việt tự nhiên cho cán bộ theo dõi hồ sơ.

        Yêu cầu bắt buộc:
        - Viết 2 đến 3 câu hoàn chỉnh.
        - Câu đầu nhận định tổng quan về tình trạng rủi ro.
        - Câu tiếp theo phải giải thích nguyên nhân dựa trên độ rộng của P80/P90.
        - Có thể sử dụng một quan hệ định lượng đáng chú ý,
          ví dụ biên P90 cao hơn dự đoán trung tâm bao nhiêu giờ
          hoặc lớn gấp bao nhiêu lần.
        - Không chỉ đọc lại toàn bộ các con số.
        - Nếu P90 lớn hơn nhiều so với dự đoán trung tâm,
          giải thích rằng độ bất định của thời gian xử lý còn đáng chú ý.
        - Nếu mức rủi ro LOW:
          nhận định hồ sơ hiện còn khoảng an toàn.
        - Nếu mức rủi ro MEDIUM:
          nhận định hồ sơ chưa ở mức nguy cấp nhưng vẫn nên theo dõi tiến độ.
        - Nếu mức rủi ro HIGH:
          nhận định nguy cơ trễ hạn đáng kể và nên ưu tiên xử lý.
        - Nếu mức rủi ro CRITICAL:
          khuyến nghị ưu tiên xử lý ngay hoặc điều phối nguồn lực.
        - Không tự thay đổi mức rủi ro đã được cung cấp.
        - Không bịa SLA, workload hoặc thông tin hồ sơ không có trong dữ liệu.
        - Không nhắc tên field kỹ thuật.
        - Không nhắc tên hoặc kết quả của các mô hình thành phần.
        - Không trả JSON.
        - Không dùng markdown.
        - Không bullet.
        - Không tiêu đề.
        - Không viết câu cụt.
        - Không viết câu chung chung kiểu "Dự kiến hồ sơ sẽ...".
        - Chỉ trả nội dung nhận xét cuối cùng.

        Ví dụ về phong cách mong muốn:
        "Hồ sơ hiện chưa ở mức nguy cơ trễ hạn cao, tuy nhiên độ bất định của thời gian xử lý vẫn đáng chú ý. Biên dự báo rộng hơn đáng kể so với giá trị dự đoán trung tâm, cho thấy thời gian hoàn tất thực tế có thể dao động. Nên tiếp tục theo dõi tiến độ để kịp thời xử lý nếu xuất hiện dấu hiệu chậm trễ."

        Không sao chép nguyên văn ví dụ trên.
        Hãy nhận xét dựa trên dữ liệu thực tế được cung cấp.
        """;
    }

    private static string CreateCacheKey(
        AiPredictionResultDto prediction)
    {
        var keySource = string.Join(
            "|",
            prediction.PredictedProcessingHours.ToString(
                "R",
                CultureInfo.InvariantCulture),
            prediction.PredictionLower80.ToString(
                "R",
                CultureInfo.InvariantCulture),
            prediction.PredictionUpper80.ToString(
                "R",
                CultureInfo.InvariantCulture),
            prediction.PredictionLower90.ToString(
                "R",
                CultureInfo.InvariantCulture),
            prediction.PredictionUpper90.ToString(
                "R",
                CultureInfo.InvariantCulture),
            prediction.PredictedCompletionTime.ToString(
                "O",
                CultureInfo.InvariantCulture),
            prediction.PredictedCompletionP90.ToString(
                "O",
                CultureInfo.InvariantCulture),
            prediction.DeadlineRisk?.Trim().ToUpperInvariant()
                ?? string.Empty);

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(keySource));

        return $"ai-insight:{Convert.ToHexString(hash)}";
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
            || parts.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "Gemini trả về response không hợp lệ.");
        }

        var textParts = new List<string>();

        foreach (var part in parts.EnumerateArray())
        {
            if (!part.TryGetProperty(
                    "text",
                    out var textElement))
            {
                continue;
            }

            var text = textElement.GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                textParts.Add(text.Trim());
            }
        }

        var insight = string.Join(
            " ",
            textParts).Trim();

        if (string.IsNullOrWhiteSpace(insight))
        {
            throw new InvalidOperationException(
                "Gemini không trả về nội dung text.");
        }

        return insight;
    }

    private void ValidateInsight(
        string insight)
    {
        string[] technicalTerms =
        [
            "CAT_RATIO",
            "XGB_LOG",
            "PROC",
            "PRIOR",
            "deadlineRisk",
            "predictionUpper",
            "Experts"
        ];

        foreach (var term in technicalTerms)
        {
            if (insight.Contains(
                    term,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Gemini insight chứa dữ liệu kỹ thuật không mong muốn: {Term}",
                    term);

                throw new InvalidOperationException(
                    "Gemini trả về nhận xét không phù hợp.");
            }
        }

        if (insight.Length < 80)
        {
            _logger.LogWarning(
                "Gemini insight quá ngắn: {Insight}",
                insight);

            throw new InvalidOperationException(
                "Gemini trả về nhận xét quá ngắn.");
        }
    }
}
