using HoSoMonitoring.Core.Models.AiPrediction;

namespace HoSoMonitoring.Core.Services;

public interface IAiPredictionService
{
    Task<AiPredictionResultDto> PredictAsync(
        AiPredictionRequestDto request,
        CancellationToken cancellationToken = default);
}
