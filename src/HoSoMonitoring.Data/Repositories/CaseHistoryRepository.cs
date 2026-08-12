using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Repositories;

public class CaseHistoryRepository
    : RepositoryBase<CaseHistory, int>, ICaseHistoryRepository
{
    public CaseHistoryRepository(HoSoMonitoringContext context)
        : base(context)
    {
    }

    public Task<List<CaseHistory>> GetByCaseIdAsync(int caseId)
    {
        return _context.CaseHistories
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
}
