using HoSoMonitoring.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("CaseHistories")]
public class CaseHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CaseId { get; set; }

    public int? UserId { get; set; }
    public CaseActionType ActionType { get; set; }
    public CaseStatus? OldStatus { get; set; }
    public CaseStatus? NewStatus { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public Case? Case { get; set; }
    public User? User { get; set; }
}
