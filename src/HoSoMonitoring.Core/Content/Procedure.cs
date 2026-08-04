using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("Procedures")]
[Index(nameof(Code), IsUnique = true)]
public class Procedure
{
    [Key]
    public int Id { get; set; }

    // Mã thủ tục dùng để định danh và liên kết với hệ thống nguồn.
    [Required]
    [Column(TypeName = "varchar(100)")]
    public required string Code { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Name { get; set; }

    [Required]
    public int ProcedureFieldId { get; set; }

    // Thời gian xử lý chuẩn của thủ tục, tính theo giờ.
    public int DefaultProcessingHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ProcedureField? ProcedureField { get; set; }
}
