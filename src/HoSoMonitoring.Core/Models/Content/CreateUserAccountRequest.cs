using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class CreateUserAccountRequest
{
    [Required, MaxLength(250)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string UserName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = string.Empty;
    [Phone, MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required] public string Role { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}
