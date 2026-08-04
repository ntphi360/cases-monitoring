using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models.Content;

public class CaseInListDto
{
    public int Id { get; set; }

    public string ExternalCaseCode { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    public DateTime Deadline { get; set; }

    public DateTime? CompletedAt { get; set; }

    public CaseStatus Status { get; set; }

    public CasePriority Priority { get; set; }

    public string? CurrentStepName { get; set; }

    public class CaseInListMappingProfile : Profile
    {
        public CaseInListMappingProfile()
        {
            CreateMap<Case, CaseInListDto>();
        }
    }
}
