namespace HoSoMonitoring.Core.Models.Import;

public class LastImportSyncDto
{
    public DateTime? LastUpdatedAt { get; set; }

    public string? FileName { get; set; }

    public bool IsStale { get; set; }

    public int StaleDataHours { get; set; }
}
