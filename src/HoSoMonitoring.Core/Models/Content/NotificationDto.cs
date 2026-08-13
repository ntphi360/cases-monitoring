namespace HoSoMonitoring.Core.Models.Content;

public class NotificationDto
{
    public int Id { get; set; }
    public int? CaseId { get; set; }
    public string? ExternalCaseCode { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
