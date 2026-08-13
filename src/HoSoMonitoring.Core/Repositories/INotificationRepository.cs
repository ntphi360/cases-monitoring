using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.SeedWorks;

namespace HoSoMonitoring.Core.Repositories;

public interface INotificationRepository : IRepository<Notification, int>
{
    Task<NotificationPageResult> GetPagingAsync(
        int? userId,
        bool? isRead,
        int pageIndex,
        int pageSize);

    Task<Notification?> GetDetailAsync(int id);
    Task<int> MarkAllReadAsync(int? userId);
}
