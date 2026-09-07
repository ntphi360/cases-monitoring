namespace HoSoMonitoring.Core.Models.AiPrediction;

public class AiPredictionResultDto
{
    public double PredictedProcessingHours { get; set; }

    public double PredictionLower80 { get; set; }

    public double PredictionUpper80 { get; set; }

    public double PredictionLower90 { get; set; }

    public double PredictionUpper90 { get; set; }

    public DateTime PredictedCompletionTime { get; set; }

    public DateTime PredictedCompletionP90 { get; set; }

    public string DeadlineRisk { get; set; } = string.Empty;

    public Dictionary<string, double> Experts { get; set; } = [];
}
