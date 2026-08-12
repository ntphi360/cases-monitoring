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

        Task<CaseAlertPageResult> GetAlertsPagingAsync(
            CaseAlertType? type,
            string? keyword,
            int? procedureFieldId,
            int? procedureId,
            int? departmentId,
            int? assignedUserId,
            int pageIndex,
            int pageSize);

        Task<DashboardSummaryDto> GetDashboardSummaryAsync();

        Task<ReportSummaryDto> GetReportSummaryAsync(ReportFilterDto filter);

        Task<bool> ExternalCaseCodeExistsAsync(string externalCaseCode);

        Task<Case?> GetDetailByIdAsync(int id);

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
