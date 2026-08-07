using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class UpdateCaseAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public int AssignedToUserId { get; set; }

    [Required]
    [MaxLength(250)]
    public string StepName { get; set; } = string.Empty;

    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [EnumDataType(typeof(AssignmentStatus))]
    public AssignmentStatus Status { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public class UpdateCaseAssignmentMappingProfile : Profile
    {
        public UpdateCaseAssignmentMappingProfile()
        {
            CreateMap<UpdateCaseAssignmentRequest, CaseAssignment>();
        }
    }
}
