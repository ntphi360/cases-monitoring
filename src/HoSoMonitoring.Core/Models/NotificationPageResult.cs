using HoSoMonitoring.Core.Models.Content;

namespace HoSoMonitoring.Core.Models;

public class NotificationPageResult
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int UnreadCount { get; set; }
    public List<NotificationDto> Results { get; set; } = [];
}
