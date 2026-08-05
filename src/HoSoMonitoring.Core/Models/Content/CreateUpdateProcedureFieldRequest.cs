using AutoMapper;
using HoSoMonitoring.Core.Content;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class CreateUpdateProcedureFieldRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public class ProcedureFieldRequestMappingProfile : Profile
    {
        public ProcedureFieldRequestMappingProfile()
        {
            CreateMap<CreateUpdateProcedureFieldRequest, ProcedureField>();
        }
    }
}
