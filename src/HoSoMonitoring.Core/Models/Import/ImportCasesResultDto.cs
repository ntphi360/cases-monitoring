namespace HoSoMonitoring.Core.Models.Import;

public class ImportCasesResultDto
{
    public int TotalRows { get; set; }

    public int InsertedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int UnchangedCount { get; set; }

    public int FailedCount { get; set; }

    public List<ImportCaseErrorDto> Errors { get; set; } = [];
}
