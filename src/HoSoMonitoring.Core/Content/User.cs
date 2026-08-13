using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoSoMonitoring.Core.Content;

[Table("Users")]
public class User : IdentityUser<int>
{
    [NotMapped]
    public string Username
    {
        get => UserName ?? string.Empty;
        set => UserName = value;
    }

    [Required]
    [MaxLength(250)]
    public required string FullName { get; set; }

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
