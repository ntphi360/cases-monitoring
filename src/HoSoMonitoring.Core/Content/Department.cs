using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("Departments")]
[Index(nameof(Code), IsUnique = true)]
public class Department
{
    [Key]
    public int Id { get; set; }

    // Mã đơn vị dùng để định danh và đồng bộ với hệ thống bên ngoài.
    [Required]
    [Column(TypeName = "varchar(50)")]
    public required string Code { get; set; }

    [Required]
    [MaxLength(250)]
    public required string Name { get; set; }

    // Đơn vị cha; null nếu đây là đơn vị cấp cao nhất.
    public int? ParentId { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Department? Parent { get; set; }
}
