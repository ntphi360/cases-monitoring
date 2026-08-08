namespace HoSoMonitoring.Core.Models.Import;

public class ImportCasesResultDto
{
    public int TotalRows { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    public List<ImportCaseErrorDto> Errors { get; set; } = [];
}
