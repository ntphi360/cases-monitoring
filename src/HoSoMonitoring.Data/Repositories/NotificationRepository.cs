using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Repositories;

public class NotificationRepository
    : RepositoryBase<Notification, int>, INotificationRepository
{
    public NotificationRepository(HoSoMonitoringContext context)
        : base(context)
    {
    }

    public async Task<NotificationPageResult> GetPagingAsync(
        int? userId,
        bool? isRead,
        int pageIndex,
        int pageSize)
    {
        var query = _context.Notifications.AsNoTracking();
        if (userId.HasValue)
        {
            query = query.Where(item => item.UserId == userId.Value);
        }

        var unreadCount = await query.CountAsync(item => !item.IsRead);
        if (isRead.HasValue)
        {
            query = query.Where(item => item.IsRead == isRead.Value);
        }

        var totalCount = await query.CountAsync();
        var results = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new NotificationDto
            {
                Id = item.Id,
                CaseId = item.CaseId,
                ExternalCaseCode = item.Case == null
                    ? null
                    : item.Case.ExternalCaseCode,
                UserId = item.UserId,
                Message = item.Message,
                IsRead = item.IsRead,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();

        return new NotificationPageResult
        {
            CurrentPage = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            UnreadCount = unreadCount,
            Results = results
        };
    }

    public Task<Notification?> GetDetailAsync(int id)
    {
        return _context.Notifications
            .AsNoTracking()
            .Include(item => item.Case)
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public Task<int> MarkAllReadAsync(int? userId)
    {
        var query = _context.Notifications.Where(item => !item.IsRead);
        if (userId.HasValue)
        {
            query = query.Where(item => item.UserId == userId.Value);
        }

        return query.ExecuteUpdateAsync(setters =>
            setters.SetProperty(item => item.IsRead, true));
    }
}
