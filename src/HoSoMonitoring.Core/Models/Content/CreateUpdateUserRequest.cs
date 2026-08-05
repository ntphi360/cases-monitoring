using AutoMapper;
using HoSoMonitoring.Core.Content;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class CreateUpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [MaxLength(100)]
    public string? ExternalUserCode { get; set; }

    public bool IsActive { get; set; }

    public class UserRequestMappingProfile : Profile
    {
        public UserRequestMappingProfile()
        {
            CreateMap<CreateUpdateUserRequest, User>();
        }
    }
}
