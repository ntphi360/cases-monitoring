using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Reminder;

public class SendReminderRequest
{
    [Range(1, int.MaxValue)]
    public int CaseId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MinLength(1)]
    public List<string> Channels { get; set; } = [];
}
