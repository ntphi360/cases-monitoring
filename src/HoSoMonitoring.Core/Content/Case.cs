using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Content;

public class Case
{
    public int Id { get; set; }

    // Mã hồ sơ tại hệ thống nguồn, dùng để chống trùng khi đồng bộ.
    public string ExternalCaseCode { get; set; } = string.Empty;

    public int ProcedureId { get; set; }

    // Đơn vị hiện chịu trách nhiệm xử lý hồ sơ.
    public int DepartmentId { get; set; }

    public DateTime ReceivedAt { get; set; }

    // Hạn hoàn thành dùng để theo dõi đúng hạn hoặc quá hạn.
    public DateTime Deadline { get; set; }

    public DateTime? CompletedAt { get; set; }

    public CaseStatus Status { get; set; }

    public CasePriority Priority { get; set; }

    // Người đang trực tiếp xử lý; null nếu hồ sơ chưa được phân công.
    public int? CurrentAssigneeId { get; set; }

    // Tên bước xử lý hiện tại lấy từ quy trình hoặc hệ thống nguồn.
    public string? CurrentStepName { get; set; }

    public DataSourceType SourceType { get; set; }

    // Thời điểm dữ liệu hồ sơ được cập nhật gần nhất tại hệ thống nguồn.
    public DateTime? ExternalUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Procedure? Procedure { get; set; }

    public Department? Department { get; set; }

    public User? CurrentAssignee { get; set; }
}
