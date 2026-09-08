using HoSoMonitoring.Core.Models.AiPrediction;

namespace HoSoMonitoring.Api.Services.Ai;

public interface IAiInsightService
{
    Task<string> GenerateInsightAsync(
        AiPredictionResultDto prediction,
        CancellationToken cancellationToken = default);
}