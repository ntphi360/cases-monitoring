using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models.Content;

public class CaseHistoryDto
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public CaseActionType ActionType { get; set; }
    public CaseStatus? OldStatus { get; set; }
    public CaseStatus? NewStatus { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public class CaseHistoryMappingProfile : Profile
    {
        public CaseHistoryMappingProfile()
        {
            CreateMap<CaseHistory, CaseHistoryDto>()
                .ForMember(
                    destination => destination.UserName,
                    options => options.MapFrom(source =>
                        source.User == null ? null : source.User.FullName));
        }
    }
}
