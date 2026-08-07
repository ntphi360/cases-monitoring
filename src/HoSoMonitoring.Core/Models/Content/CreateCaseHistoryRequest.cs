using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class CreateCaseHistoryRequest
{
    [Range(1, int.MaxValue)]
    public int CaseId { get; set; }

    [Range(1, int.MaxValue)]
    public int? UserId { get; set; }

    [EnumDataType(typeof(CaseActionType))]
    public CaseActionType ActionType { get; set; }

    [EnumDataType(typeof(CaseStatus))]
    public CaseStatus? OldStatus { get; set; }

    [EnumDataType(typeof(CaseStatus))]
    public CaseStatus? NewStatus { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public class CreateCaseHistoryMappingProfile : Profile
    {
        public CreateCaseHistoryMappingProfile()
        {
            CreateMap<CreateCaseHistoryRequest, CaseHistory>();
        }
    }
}
