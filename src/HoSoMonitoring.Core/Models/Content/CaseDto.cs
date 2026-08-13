using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models.Content;

public class CaseDto : CaseInListDto
{
    public int ProcedureId { get; set; }

    public int DepartmentId { get; set; }

    public DataSourceType SourceType { get; set; }

    public DateTime? ExternalUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CityCode { get; set; }

    public string? CityName { get; set; }

    public string? WardCode { get; set; }

    public string? WardName { get; set; }

    public DateTime? CaseCodeDate { get; set; }

    public int? DailySequence { get; set; }

    public class CaseMappingProfile : Profile
    {
        public CaseMappingProfile()
        {
            CreateMap<Case, CaseDto>()
                .IncludeBase<Case, CaseInListDto>()
                .ForMember(destination => destination.CityCode, options => options.Ignore())
                .ForMember(destination => destination.CityName, options => options.Ignore())
                .ForMember(destination => destination.WardCode, options => options.Ignore())
                .ForMember(destination => destination.WardName, options => options.Ignore())
                .ForMember(destination => destination.CaseCodeDate, options => options.Ignore())
                .ForMember(destination => destination.DailySequence, options => options.Ignore());
        }
    }
}
