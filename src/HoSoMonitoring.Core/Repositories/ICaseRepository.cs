using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;

namespace HoSoMonitoring.Core.Repositories
{
    public interface ICaseRepository : IRepository<Case, int>
    {
        Task<List<Case>> GetOverdueCasesAsync(int count);

        Task<PageResult<CaseInListDto>> GetAllPagingAsync(
            string? keyword,
            int pageIndex,
            int pageSize);
    }
}