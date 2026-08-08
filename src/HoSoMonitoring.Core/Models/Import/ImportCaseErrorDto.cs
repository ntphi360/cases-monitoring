namespace HoSoMonitoring.Core.Models.Import;

public class ImportCaseErrorDto
{
    public int Row { get; set; }

    public string? ExternalCaseCode { get; set; }

    public string Message { get; set; } = string.Empty;
}
