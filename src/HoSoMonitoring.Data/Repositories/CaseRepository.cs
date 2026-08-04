using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Repositories
{
    public class CaseRepository
        : RepositoryBase<Case, int>, ICaseRepository
    {
        public CaseRepository(HoSoMonitoringContext context)
            : base(context)
        {
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
    }
}