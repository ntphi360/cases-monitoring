using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Repositories;

public class CaseAssignmentRepository
    : RepositoryBase<CaseAssignment, int>, ICaseAssignmentRepository
{
    public CaseAssignmentRepository(HoSoMonitoringContext context)
        : base(context)
    {
    }

    public Task<List<CaseAssignment>> GetByCaseIdAsync(int caseId)
    {
        return _context.CaseAssignments
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.AssignedAt)
            .ToListAsync();
    }
}
