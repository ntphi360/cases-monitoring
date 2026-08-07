using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models.Content;

public class CaseAssignmentDto
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int AssignedToUserId { get; set; }
    public int? AssignedByUserId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AssignmentStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public class CaseAssignmentMappingProfile : Profile
    {
        public CaseAssignmentMappingProfile()
        {
            CreateMap<CaseAssignment, CaseAssignmentDto>();
        }
    }
}
