using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("Notifications")]
[Index(nameof(UserId), nameof(IsRead), nameof(CreatedAt))]
public class Notification
{
    [Key]
    public int Id { get; set; }
    public int? CaseId { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public Case? Case { get; set; }
    public User? User { get; set; }
}
