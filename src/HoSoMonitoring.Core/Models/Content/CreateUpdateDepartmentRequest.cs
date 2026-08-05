using AutoMapper;
using HoSoMonitoring.Core.Content;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class CreateUpdateDepartmentRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    public bool IsActive { get; set; }

    public class DepartmentRequestMappingProfile : Profile
    {
        public DepartmentRequestMappingProfile()
        {
            CreateMap<CreateUpdateDepartmentRequest, Department>();
        }
    }
}
