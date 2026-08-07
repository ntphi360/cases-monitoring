using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.SeedWorks;

namespace HoSoMonitoring.Core.Repositories;

public interface ICaseHistoryRepository : IRepository<CaseHistory, int>
{
    Task<List<CaseHistory>> GetByCaseIdAsync(int caseId);
}
