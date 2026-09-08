using HoSoMonitoring.Core.Models.AiPrediction;

namespace HoSoMonitoring.Api.Models.AiPrediction;

public sealed class CaseAiPredictionResponseDto : AiPredictionResultDto
{
    public string AiInsight { get; set; } = string.Empty;

    public static CaseAiPredictionResponseDto FromPrediction(
        AiPredictionResultDto prediction,
        string aiInsight)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        return new CaseAiPredictionResponseDto
        {
            PredictedProcessingHours = prediction.PredictedProcessingHours,
            PredictionLower80 = prediction.PredictionLower80,
            PredictionUpper80 = prediction.PredictionUpper80,
            PredictionLower90 = prediction.PredictionLower90,
            PredictionUpper90 = prediction.PredictionUpper90,
            PredictedCompletionTime = prediction.PredictedCompletionTime,
            PredictedCompletionP90 = prediction.PredictedCompletionP90,
            DeadlineRisk = prediction.DeadlineRisk,
            Experts = prediction.Experts,
            AiInsight = aiInsight
        };
    }
}
