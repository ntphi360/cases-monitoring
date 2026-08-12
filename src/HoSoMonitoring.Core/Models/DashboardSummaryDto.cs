using HoSoMonitoring.Core.Enums;
using HoSoMonitoring.Core.Models.Content;

namespace HoSoMonitoring.Core.Models;

public class DashboardSummaryDto
{
    public int TotalCases { get; set; }

    public int NearDeadlineCases { get; set; }

    public int OverdueCases { get; set; }

    public int CompletedCases { get; set; }

    public List<DashboardStatusItemDto> StatusDistribution { get; set; } = new();

    public List<DashboardTrendItemDto> Trend { get; set; } = new();

    public List<CaseInListDto> RecentCases { get; set; } = new();
}

public class DashboardStatusItemDto
{
    public CaseStatus Status { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class DashboardTrendItemDto
{
    public string Period { get; set; } = string.Empty;

    public int Received { get; set; }

    public int Completed { get; set; }
}
