using HoSoMonitoring.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("CaseAssignments")]
public class CaseAssignment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CaseId { get; set; }

    [Required]
    public int AssignedToUserId { get; set; }

    public int? AssignedByUserId { get; set; }

    [Required]
    [MaxLength(250)]
    public required string StepName { get; set; }

    public DateTime AssignedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AssignmentStatus Status { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public Case? Case { get; set; }
    public User? AssignedToUser { get; set; }
    public User? AssignedByUser { get; set; }
}
