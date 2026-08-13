using HoSoMonitoring.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("ReminderDeliveries")]
public class ReminderDelivery
{
    [Key]
    public int Id { get; set; }

    public int CaseId { get; set; }

    public int UserId { get; set; }

    public ReminderChannel Channel { get; set; }

    [MaxLength(250)]
    public string? Recipient { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public ReminderDeliveryStatus Status { get; set; }

    public DateTime SentAt { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public Case? Case { get; set; }

    public User? User { get; set; }
}
