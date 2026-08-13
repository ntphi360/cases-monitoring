using AutoMapper;
using AutoMapper.QueryableExtensions;
using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Repositories
{
    public class CaseRepository
        : RepositoryBase<Case, int>, ICaseRepository
    {
        private const int AlertWarningThresholdDays = 14;

        private readonly IMapper _mapper;
        private readonly AdministrativeUnitOptions _administrativeUnit;

        public CaseRepository(
            HoSoMonitoringContext context,
            IMapper mapper,
            AdministrativeUnitOptions administrativeUnit)
            : base(context)
        {
            _mapper = mapper;
            _administrativeUnit = administrativeUnit;
        }

        public async Task<List<Case>> GetOverdueCasesAsync(int count)
        {
            return await _context.Cases
                .Where(x =>
                    x.Deadline < DateTime.Now &&
                    x.CompletedAt == null)
                .OrderBy(x => x.Deadline)
                .Take(count)
                .ToListAsync();
        }

        public Task<bool> ExternalCaseCodeExistsAsync(string externalCaseCode)
        {
            return _context.Cases.AnyAsync(item =>
                item.ExternalCaseCode == externalCaseCode);
        }

        public Task<Case?> GetDetailByIdAsync(int id)
        {
            return _context.Cases
                .AsNoTracking()
                .Include(item => item.Procedure)
                    .ThenInclude(procedure => procedure!.ProcedureField)
                .Include(item => item.Department)
                .Include(item => item.CurrentAssignee)
                .FirstOrDefaultAsync(item => item.Id == id);
        }

        public async Task<List<CaseExportDto>> GetForExportAsync(
            string? keyword,
            int? departmentId,
            int? procedureFieldId,
            int? procedureId,
            int? assignedUserId,
            CaseStatus? status,
            DateTime? receivedFrom,
            DateTime? receivedTo)
        {
            var query = _context.Cases.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(item =>
                    item.ExternalCaseCode.Contains(keyword)
                    || item.ApplicantName.Contains(keyword));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(item => item.DepartmentId == departmentId.Value);
            }

            if (procedureFieldId.HasValue)
            {
                query = query.Where(item =>
                    item.Procedure!.ProcedureFieldId == procedureFieldId.Value);
            }

            if (procedureId.HasValue)
            {
                query = query.Where(item => item.ProcedureId == procedureId.Value);
            }

            if (assignedUserId.HasValue)
            {
                query = query.Where(item =>
                    item.CurrentAssigneeId == assignedUserId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(item => item.Status == status.Value);
            }

            if (receivedFrom.HasValue)
            {
                query = query.Where(item =>
                    item.ReceivedAt >= receivedFrom.Value.Date);
            }

            if (receivedTo.HasValue)
            {
                var toExclusive = receivedTo.Value.Date.AddDays(1);
                query = query.Where(item => item.ReceivedAt < toExclusive);
            }

            var rows = await query
                .OrderByDescending(item => item.ReceivedAt)
                .Select(item => new CaseExportDto
                {
                    ExternalCaseCode = item.ExternalCaseCode,
                    ApplicantName = item.ApplicantName,
                    ProcedureFieldName = item.Procedure!.ProcedureField!.Name,
                    ProcedureName = item.Procedure.Name,
                    DepartmentName = item.Department!.Name,
                    OrganizationName = item.OrganizationName,
                    AssigneeName = item.CurrentAssignee == null
                        ? null
                        : item.CurrentAssignee.FullName,
                    ReceivedAt = item.ReceivedAt,
                    Deadline = item.Deadline,
                    AppointmentDate = item.AppointmentDate,
                    CompletedAt = item.CompletedAt,
                    Status = item.Status
                })
                .ToListAsync();

            foreach (var row in rows)
            {
                row.OrganizationName = _administrativeUnit.OrganizationName;
            }

            return rows;
        }

        public async Task<CaseAlertPageResult> GetAlertsPagingAsync(
            CaseAlertType? type,
            string? keyword,
            int? procedureFieldId,
            int? procedureId,
            int? departmentId,
            int? assignedUserId,
            int pageIndex,
            int pageSize)
        {
            var now = DateTime.Now;
            var tomorrow = now.Date.AddDays(1);
            var warningDeadline = now.AddDays(AlertWarningThresholdDays);

            var baseQuery = _context.Cases
                .AsNoTracking()
                .Where(item =>
                    item.CompletedAt == null
                    && item.Status != CaseStatus.Completed
                    && item.Status != CaseStatus.Cancelled
                    && item.Deadline <= warningDeadline);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                baseQuery = baseQuery.Where(item =>
                    item.ExternalCaseCode.Contains(keyword)
                    || item.ApplicantName.Contains(keyword));
            }

            if (procedureFieldId.HasValue)
            {
                baseQuery = baseQuery.Where(item =>
                    item.Procedure!.ProcedureFieldId == procedureFieldId.Value);
            }

            if (procedureId.HasValue)
            {
                baseQuery = baseQuery.Where(item =>
                    item.ProcedureId == procedureId.Value);
            }

            if (departmentId.HasValue)
            {
                baseQuery = baseQuery.Where(item =>
                    item.DepartmentId == departmentId.Value);
            }

            if (assignedUserId.HasValue)
            {
                baseQuery = baseQuery.Where(item =>
                    item.CurrentAssigneeId == assignedUserId.Value);
            }

            var upcomingCount = await baseQuery.CountAsync(item =>
                item.Deadline >= tomorrow);
            var dueTodayCount = await baseQuery.CountAsync(item =>
                item.Deadline >= now && item.Deadline < tomorrow);
            var overdueCount = await baseQuery.CountAsync(item =>
                item.Deadline < now);

            var query = type switch
            {
                CaseAlertType.Upcoming => baseQuery.Where(item =>
                    item.Deadline >= tomorrow),
                CaseAlertType.DueToday => baseQuery.Where(item =>
                    item.Deadline >= now && item.Deadline < tomorrow),
                CaseAlertType.Overdue => baseQuery.Where(item =>
                    item.Deadline < now),
                _ => baseQuery
            };

            var totalCount = await query.CountAsync();
            var cases = await query
                .OrderBy(item => item.Deadline)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<CaseInListDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            foreach (var caseDto in cases)
            {
                caseDto.OrganizationName = _administrativeUnit.OrganizationName;
            }

            return new CaseAlertPageResult
            {
                CurrentPage = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                UpcomingCount = upcomingCount,
                DueTodayCount = dueTodayCount,
                OverdueCount = overdueCount,
                Results = cases
            };
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var now = DateTime.Now;
            var warningDeadline = now.AddDays(AlertWarningThresholdDays);
            var casesQuery = _context.Cases.AsNoTracking();

            var summary = await casesQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Completed = group.Count(item =>
                        item.Status == CaseStatus.Completed),
                    Overdue = group.Count(item =>
                        item.CompletedAt == null
                        && item.Status != CaseStatus.Completed
                        && item.Status != CaseStatus.Cancelled
                        && item.Deadline < now),
                    NearDeadline = group.Count(item =>
                        item.CompletedAt == null
                        && item.Status != CaseStatus.Completed
                        && item.Status != CaseStatus.Cancelled
                        && item.Deadline >= now
                        && item.Deadline <= warningDeadline)
                })
                .SingleOrDefaultAsync();

            var statusCounts = await casesQuery
                .GroupBy(item => item.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .OrderBy(item => item.Status)
                .ToListAsync();

            var firstTrendMonth = new DateTime(now.Year, now.Month, 1)
                .AddMonths(-6);
            var receivedByMonth = await casesQuery
                .Where(item => item.ReceivedAt >= firstTrendMonth)
                .GroupBy(item => new
                {
                    item.ReceivedAt.Year,
                    item.ReceivedAt.Month
                })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Count = group.Count()
                })
                .ToListAsync();
            var completedByMonth = await casesQuery
                .Where(item => item.CompletedAt >= firstTrendMonth)
                .GroupBy(item => new
                {
                    Year = item.CompletedAt!.Value.Year,
                    Month = item.CompletedAt.Value.Month
                })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Count = group.Count()
                })
                .ToListAsync();

            var trend = Enumerable.Range(0, 7)
                .Select(offset => firstTrendMonth.AddMonths(offset))
                .Select(month => new DashboardTrendItemDto
                {
                    Period = month.ToString("MM/yyyy"),
                    Received = receivedByMonth
                        .FirstOrDefault(item =>
                            item.Year == month.Year
                            && item.Month == month.Month)?.Count ?? 0,
                    Completed = completedByMonth
                        .FirstOrDefault(item =>
                            item.Year == month.Year
                            && item.Month == month.Month)?.Count ?? 0
                })
                .ToList();

            var recentCases = await casesQuery
                .OrderByDescending(item => item.ReceivedAt)
                .Take(5)
                .ProjectTo<CaseInListDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            foreach (var caseDto in recentCases)
            {
                caseDto.OrganizationName = _administrativeUnit.OrganizationName;
            }

            return new DashboardSummaryDto
            {
                TotalCases = summary?.Total ?? 0,
                NearDeadlineCases = summary?.NearDeadline ?? 0,
                OverdueCases = summary?.Overdue ?? 0,
                CompletedCases = summary?.Completed ?? 0,
                StatusDistribution = statusCounts
                    .Select(item => new DashboardStatusItemDto
                    {
                        Status = item.Status,
                        Key = item.Status.ToString(),
                        Label = GetCaseStatusLabel(item.Status),
                        Count = item.Count
                    })
                    .ToList(),
                Trend = trend,
                RecentCases = recentCases
            };
        }

        public async Task<ReportSummaryDto> GetReportSummaryAsync(
            ReportFilterDto filter)
        {
            var now = DateTime.Now;
            var query = _context.Cases.AsNoTracking();

            if (filter.From.HasValue)
            {
                query = query.Where(item =>
                    item.ReceivedAt >= filter.From.Value.Date);
            }

            if (filter.To.HasValue)
            {
                var toExclusive = filter.To.Value.Date.AddDays(1);
                query = query.Where(item => item.ReceivedAt < toExclusive);
            }

            if (filter.ProcedureFieldId.HasValue)
            {
                query = query.Where(item =>
                    item.Procedure!.ProcedureFieldId
                    == filter.ProcedureFieldId.Value);
            }

            if (filter.ProcedureId.HasValue)
            {
                query = query.Where(item =>
                    item.ProcedureId == filter.ProcedureId.Value);
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(item =>
                    item.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.AssignedUserId.HasValue)
            {
                query = query.Where(item =>
                    item.CurrentAssigneeId == filter.AssignedUserId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(item =>
                    item.Status == filter.Status.Value);
            }

            var summary = await query
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Total = group.Count(),
                    Completed = group.Count(item =>
                        item.Status == CaseStatus.Completed),
                    Processing = group.Count(item =>
                        item.Status == CaseStatus.InProgress),
                    Overdue = group.Count(item =>
                        item.CompletedAt == null
                        && item.Status != CaseStatus.Completed
                        && item.Status != CaseStatus.Cancelled
                        && item.Deadline < now)
                })
                .SingleOrDefaultAsync();

            var byProcedureField = await query
                .GroupBy(item => new
                {
                    Id = item.Procedure!.ProcedureFieldId,
                    Name = item.Procedure.ProcedureField!.Name
                })
                .Select(group => new ReportGroupItemDto
                {
                    Id = group.Key.Id,
                    Name = group.Key.Name,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToListAsync();

            var byProcedure = await query
                .GroupBy(item => new
                {
                    Id = item.ProcedureId,
                    Name = item.Procedure!.Name
                })
                .Select(group => new ReportGroupItemDto
                {
                    Id = group.Key.Id,
                    Name = group.Key.Name,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToListAsync();

            var byDepartment = await query
                .GroupBy(item => new
                {
                    Id = item.DepartmentId,
                    Name = item.Department!.Name
                })
                .Select(group => new ReportGroupItemDto
                {
                    Id = group.Key.Id,
                    Name = group.Key.Name,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToListAsync();

            var byAssignee = await query
                .GroupBy(item => new
                {
                    Id = item.CurrentAssigneeId,
                    Name = item.CurrentAssignee == null
                        ? "Chưa phân công"
                        : item.CurrentAssignee.FullName
                })
                .Select(group => new ReportGroupItemDto
                {
                    Id = group.Key.Id,
                    Name = group.Key.Name,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ToListAsync();

            var useDailyTrend = filter.From.HasValue
                && filter.To.HasValue
                && (filter.To.Value.Date - filter.From.Value.Date).TotalDays <= 62;

            List<ReportTrendItemDto> trend;
            if (useDailyTrend)
            {
                var received = await query
                    .GroupBy(item => item.ReceivedAt.Date)
                    .Select(group => new
                    {
                        Date = group.Key,
                        Count = group.Count()
                    })
                    .ToListAsync();
                var completed = await query
                    .Where(item => item.CompletedAt.HasValue)
                    .GroupBy(item => item.CompletedAt!.Value.Date)
                    .Select(group => new
                    {
                        Date = group.Key,
                        Count = group.Count()
                    })
                    .ToListAsync();

                trend = received.Select(item => item.Date)
                    .Concat(completed.Select(item => item.Date))
                    .Distinct()
                    .OrderBy(date => date)
                    .Select(date => new ReportTrendItemDto
                    {
                        Period = date.ToString("yyyy-MM-dd"),
                        ReceivedCount = received
                            .FirstOrDefault(item => item.Date == date)?.Count ?? 0,
                        CompletedCount = completed
                            .FirstOrDefault(item => item.Date == date)?.Count ?? 0
                    })
                    .ToList();
            }
            else
            {
                var received = await query
                    .GroupBy(item => new
                    {
                        item.ReceivedAt.Year,
                        item.ReceivedAt.Month
                    })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,
                        Count = group.Count()
                    })
                    .ToListAsync();
                var completed = await query
                    .Where(item => item.CompletedAt.HasValue)
                    .GroupBy(item => new
                    {
                        Year = item.CompletedAt!.Value.Year,
                        Month = item.CompletedAt.Value.Month
                    })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,
                        Count = group.Count()
                    })
                    .ToListAsync();

                trend = received
                    .Select(item => (item.Year, item.Month))
                    .Concat(completed.Select(item => (item.Year, item.Month)))
                    .Distinct()
                    .OrderBy(item => item.Year)
                    .ThenBy(item => item.Month)
                    .Select(period => new ReportTrendItemDto
                    {
                        Period = $"{period.Month:00}/{period.Year}",
                        ReceivedCount = received.FirstOrDefault(item =>
                            item.Year == period.Year
                            && item.Month == period.Month)?.Count ?? 0,
                        CompletedCount = completed.FirstOrDefault(item =>
                            item.Year == period.Year
                            && item.Month == period.Month)?.Count ?? 0
                    })
                    .ToList();
            }

            return new ReportSummaryDto
            {
                TotalCases = summary?.Total ?? 0,
                CompletedCases = summary?.Completed ?? 0,
                ProcessingCases = summary?.Processing ?? 0,
                OverdueCases = summary?.Overdue ?? 0,
                TrendGranularity = useDailyTrend ? "day" : "month",
                ByProcedureField = byProcedureField,
                ByProcedure = byProcedure,
                ByDepartment = byDepartment,
                ByAssignee = byAssignee,
                Trend = trend
            };
        }

        private static string GetCaseStatusLabel(CaseStatus status)
        {
            return status switch
            {
                CaseStatus.Received => "Mới tiếp nhận",
                CaseStatus.InProgress => "Đang xử lý",
                CaseStatus.Pending => "Chờ xử lý",
                CaseStatus.Completed => "Đã hoàn thành",
                CaseStatus.Overdue => "Quá hạn",
                CaseStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        public async Task<PageResult<CaseInListDto>> GetAllPagingAsync(
            string? keyword,
            int? departmentId,
            int? procedureFieldId,
            int? procedureId,
            int? assignedUserId,
            CaseStatus? status,
            DateTime? receivedFrom,
            DateTime? receivedTo,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Cases.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.ExternalCaseCode.Contains(keyword)
                    || x.ApplicantName.Contains(keyword));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(x =>
                    x.DepartmentId == departmentId.Value);
            }

            if (procedureFieldId.HasValue)
            {
                query = query.Where(x =>
                    x.Procedure!.ProcedureFieldId == procedureFieldId.Value);
            }

            if (procedureId.HasValue)
            {
                query = query.Where(x =>
                    x.ProcedureId == procedureId.Value);
            }

            if (assignedUserId.HasValue)
            {
                query = query.Where(x =>
                    x.CurrentAssigneeId == assignedUserId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x =>
                    x.Status == status.Value);
            }

            if (receivedFrom.HasValue)
            {
                query = query.Where(x =>
                    x.ReceivedAt >= receivedFrom.Value.Date);
            }

            if (receivedTo.HasValue)
            {
                var receivedToExclusive = receivedTo.Value.Date.AddDays(1);
                query = query.Where(x =>
                    x.ReceivedAt < receivedToExclusive);
            }

            var totalCount = await query.CountAsync();

            var cases = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<CaseInListDto>(
                    _mapper.ConfigurationProvider)
                .ToListAsync();

            foreach (var caseDto in cases)
            {
                caseDto.OrganizationName = _administrativeUnit.OrganizationName;
            }

            return new PageResult<CaseInListDto>
            {
                CurrentPage = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(
                    totalCount / (double)pageSize),
                Results = cases
            };
        }
    }
}
