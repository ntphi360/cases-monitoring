namespace HoSoMonitoring.Core.Models.Content;

public record CaseCodeInfo
{
    public string OriginalCode { get; init; } = string.Empty;

    public string CityCode { get; init; } = string.Empty;

    public string? CityName { get; init; }

    public string WardCode { get; init; } = string.Empty;

    public string? WardName { get; init; }

    public DateTime? ReceivedDate { get; init; }

    public int? DailySequence { get; init; }

    public bool IsValid { get; init; }

    public string? ErrorMessage { get; init; }
}
