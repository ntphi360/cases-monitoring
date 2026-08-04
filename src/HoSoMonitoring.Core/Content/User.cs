using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("Users")]
[Index(nameof(Username), IsUnique = true)]
public class User
{
    [Key]
    public int Id { get; set; }

    // Tên đăng nhập duy nhất trong hệ thống.
    [Required]
    [Column(TypeName = "varchar(100)")]
    public required string Username { get; set; }

    [Required]
    [MaxLength(250)]
    public required string FullName { get; set; }

    [Required]
    [Column(TypeName = "varchar(256)")]
    public required string Email { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string? PhoneNumber { get; set; }

    // Đơn vị mà người dùng đang công tác.
    [Required]
    public int DepartmentId { get; set; }

    // Mã người dùng tại hệ thống nguồn, phục vụ đối soát và đồng bộ.
    [Column(TypeName = "varchar(100)")]
    public string? ExternalUserCode { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Department? Department { get; set; }
}
