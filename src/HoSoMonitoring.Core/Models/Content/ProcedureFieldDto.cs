using AutoMapper;
using HoSoMonitoring.Core.Content;

namespace HoSoMonitoring.Core.Models.Content;

public class ProcedureFieldDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public class ProcedureFieldMappingProfile : Profile
    {
        public ProcedureFieldMappingProfile()
        {
            CreateMap<ProcedureField, ProcedureFieldDto>();
        }
    }
}
