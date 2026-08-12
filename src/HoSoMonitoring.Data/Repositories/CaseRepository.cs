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
                    x.Procedure!.DepartmentId == departmentId.Value);
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
