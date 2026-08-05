using AutoMapper;
using HoSoMonitoring.Core.Content;

namespace HoSoMonitoring.Core.Models.Content;

public class ProcedureDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProcedureFieldId { get; set; }
    public int DefaultProcessingHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public class ProcedureMappingProfile : Profile
    {
        public ProcedureMappingProfile()
        {
            CreateMap<Procedure, ProcedureDto>();
        }
    }
}
