using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;

namespace HoSoMonitoring.Core.Repositories
{
    public interface ICaseRepository : IRepository<Case, int>
    {
        Task<List<Case>> GetOverdueCasesAsync(int count);

        Task<bool> ExternalCaseCodeExistsAsync(string externalCaseCode);

        Task<PageResult<CaseInListDto>> GetAllPagingAsync(
            string? keyword,
            int? departmentId,
            int? procedureFieldId,
            int? procedureId,
            int? assignedUserId,
            CaseStatus? status,
            DateTime? receivedFrom,
            DateTime? receivedTo,
            int pageIndex,
            int pageSize);
    }
}
