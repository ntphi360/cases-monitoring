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
            int? procedureId,
            CaseStatus? status,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Cases.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.ExternalCaseCode.Contains(keyword));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(x =>
                    x.DepartmentId == departmentId.Value);
            }

            if (procedureId.HasValue)
            {
                query = query.Where(x =>
                    x.ProcedureId == procedureId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x =>
                    x.Status == status.Value);
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
