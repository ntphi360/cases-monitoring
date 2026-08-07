using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.SeedWorks;

namespace HoSoMonitoring.Core.Repositories;

public interface ICaseAssignmentRepository : IRepository<CaseAssignment, int>
{
    Task<List<CaseAssignment>> GetByCaseIdAsync(int caseId);
}
