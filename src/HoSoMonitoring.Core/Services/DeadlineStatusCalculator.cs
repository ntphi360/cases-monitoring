using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Services;

public static class DeadlineStatusCalculator
{
    public static DeadlineStatus Calculate(
        Case item,
        DateTime now,
        int warningThresholdDays)
    {
        if (item.Status == CaseStatus.Completed)
        {
            return item.CompletedAt.HasValue && item.CompletedAt.Value > item.Deadline
                ? DeadlineStatus.CompletedLate
                : DeadlineStatus.CompletedOnTime;
        }

        if (item.Status == CaseStatus.Cancelled)
        {
            return DeadlineStatus.NotApplicable;
        }

        if (item.Deadline < now)
        {
            return DeadlineStatus.Overdue;
        }

        if (item.Deadline.Date == now.Date)
        {
            return DeadlineStatus.DueToday;
        }

        return item.Deadline <= now.AddDays(warningThresholdDays)
            ? DeadlineStatus.NearDeadline
            : DeadlineStatus.OnTime;
    }
}
