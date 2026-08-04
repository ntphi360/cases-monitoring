using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models.Content;

public class CaseDto : CaseInListDto
{
    public int ProcedureId { get; set; }

    public int DepartmentId { get; set; }

    public int? CurrentAssigneeId { get; set; }

    public DataSourceType SourceType { get; set; }

    public DateTime? ExternalUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public class CaseMappingProfile : Profile
    {
        public CaseMappingProfile()
        {
            CreateMap<Case, CaseDto>();
        }
    }
}
