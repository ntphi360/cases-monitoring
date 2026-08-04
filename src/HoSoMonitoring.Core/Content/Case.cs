using HoSoMonitoring.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("Cases")]
[Index(nameof(ExternalCaseCode), IsUnique = true)]
public class Case
{
    [Key]
    public int Id { get; set; }

    // Mã hồ sơ tại hệ thống nguồn, dùng để chống trùng khi đồng bộ.
    [Required]
    [Column(TypeName = "varchar(100)")]
    public required string ExternalCaseCode { get; set; }

    [Required]
    public int ProcedureId { get; set; }

    // Đơn vị hiện chịu trách nhiệm xử lý hồ sơ.
    [Required]
    public int DepartmentId { get; set; }

    public DateTime ReceivedAt { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime? CompletedAt { get; set; }
    public CaseStatus Status { get; set; }
    public CasePriority Priority { get; set; }
    public int? CurrentAssigneeId { get; set; }

    // Tên bước xử lý hiện tại lấy từ quy trình hoặc hệ thống nguồn.
    [MaxLength(250)]
    public string? CurrentStepName { get; set; }

    public DataSourceType SourceType { get; set; }
    public DateTime? ExternalUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Procedure? Procedure { get; set; }
    public Department? Department { get; set; }
    public User? CurrentAssignee { get; set; }
}
