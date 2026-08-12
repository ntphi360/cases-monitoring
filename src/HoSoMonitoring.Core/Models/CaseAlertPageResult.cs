using HoSoMonitoring.Core.Models.Content;

namespace HoSoMonitoring.Core.Models;

public class CaseAlertPageResult : PageResult<CaseInListDto>
{
    public int UpcomingCount { get; set; }

    public int DueTodayCount { get; set; }

    public int OverdueCount { get; set; }
}
