using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Content;

public class CaseHistory
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    // Người thực hiện hành động; null nếu hành động do hệ thống tạo.
    public int? UserId { get; set; }

    public CaseActionType ActionType { get; set; }

    // Trạng thái trước và sau có thể null với hành động không đổi trạng thái.
    public CaseStatus? OldStatus { get; set; }

    public CaseStatus? NewStatus { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public Case? Case { get; set; }

    public User? User { get; set; }
}
