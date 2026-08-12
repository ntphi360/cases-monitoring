using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models;

public class ReportSummaryDto
{
    public int TotalCases { get; set; }
    public int CompletedCases { get; set; }
    public int ProcessingCases { get; set; }
    public int OverdueCases { get; set; }
    public string TrendGranularity { get; set; } = "month";
    public List<ReportGroupItemDto> ByProcedureField { get; set; } = [];
    public List<ReportGroupItemDto> ByProcedure { get; set; } = [];
    public List<ReportGroupItemDto> ByDepartment { get; set; } = [];
    public List<ReportGroupItemDto> ByAssignee { get; set; } = [];
    public List<ReportTrendItemDto> Trend { get; set; } = [];
}

public class ReportGroupItemDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReportTrendItemDto
{
    public string Period { get; set; } = string.Empty;
    public int ReceivedCount { get; set; }
    public int CompletedCount { get; set; }
}

public class ReportFilterDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? ProcedureFieldId { get; set; }
    public int? ProcedureId { get; set; }
    public int? DepartmentId { get; set; }
    public int? AssignedUserId { get; set; }
    public CaseStatus? Status { get; set; }
}
