using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Content;

public class CaseAssignment
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    // Người được giao xử lý hồ sơ tại bước này.
    public int AssignedToUserId { get; set; }

    // Người thực hiện phân công; null nếu hệ thống tự động phân công.
    public int? AssignedByUserId { get; set; }

    public string StepName { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; }

    // Hạn xử lý riêng của lần phân công, có thể khác hạn tổng thể của hồ sơ.
    public DateTime? DueAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public AssignmentStatus Status { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public Case? Case { get; set; }

    public User? AssignedToUser { get; set; }

    public User? AssignedByUser { get; set; }
}
