namespace HoSoMonitoring.Core.Models.AiPrediction;

public class AiPredictionRequestDto
{
    public string ProcedureName { get; set; } = string.Empty;

    public string FieldName { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string OfficerName { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    public DateTime? DueAt { get; set; }
}
