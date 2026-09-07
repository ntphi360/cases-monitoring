namespace HoSoMonitoring.Core.Configurations;

public class AiPredictionOptions
{
    public const string SectionName = "AiPrediction";

    public string PythonExecutable { get; set; } = "python";

    public string ScriptPath { get; set; } = "../../ai/predict.py";

    public int TimeoutSeconds { get; set; } = 30;
}
