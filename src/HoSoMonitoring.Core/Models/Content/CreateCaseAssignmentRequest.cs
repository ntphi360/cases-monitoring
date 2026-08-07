using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content;

public class CreateCaseAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public int CaseId { get; set; }

    [Range(1, int.MaxValue)]
    public int AssignedToUserId { get; set; }

    [Range(1, int.MaxValue)]
    public int? AssignedByUserId { get; set; }

    [Required]
    [MaxLength(250)]
    public string StepName { get; set; } = string.Empty;

    public DateTime? DueAt { get; set; }

    [EnumDataType(typeof(AssignmentStatus))]
    public AssignmentStatus Status { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public class CreateCaseAssignmentMappingProfile : Profile
    {
        public CreateCaseAssignmentMappingProfile()
        {
            CreateMap<CreateCaseAssignmentRequest, CaseAssignment>();
        }
    }
}
