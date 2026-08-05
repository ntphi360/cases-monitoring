using AutoMapper;
using HoSoMonitoring.Core.Content;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class CreateUpdateProcedureRequest
{
    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ProcedureFieldId { get; set; }

    [Range(0, int.MaxValue)]
    public int DefaultProcessingHours { get; set; }

    public bool IsActive { get; set; }

    public class ProcedureRequestMappingProfile : Profile
    {
        public ProcedureRequestMappingProfile()
        {
            CreateMap<CreateUpdateProcedureRequest, Procedure>();
        }
    }
}
