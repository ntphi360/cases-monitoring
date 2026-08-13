using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models.Content;

public class CaseInListDto
{
    public int Id { get; set; }

    public string ExternalCaseCode { get; set; } = string.Empty;

    public string ApplicantName { get; set; } = string.Empty;

    public string? ProcedureName { get; set; }

    public string? ProcedureFieldName { get; set; }

    public string? DepartmentName { get; set; }

    public string? OrganizationName { get; set; }

    public string? AssigneeName { get; set; }

    public int? CurrentAssigneeId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public DateTime Deadline { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? ProcessingDays { get; set; }

    public CaseStatus Status { get; set; }

    public DeadlineStatus DeadlineStatus { get; set; }

    public CasePriority Priority { get; set; }

    public string? CurrentStepName { get; set; }

    public class CaseInListMappingProfile : Profile
    {
        public CaseInListMappingProfile()
        {
            CreateMap<Case, CaseInListDto>()
                .ForMember(
                    destination => destination.DeadlineStatus,
                    options => options.Ignore())
                .ForMember(
                    destination => destination.ProcedureName,
                    options => options.MapFrom(source => source.Procedure!.Name))
                .ForMember(
                    destination => destination.ProcedureFieldName,
                    options => options.MapFrom(source => source.Procedure!.ProcedureField!.Name))
                .ForMember(
                    destination => destination.DepartmentName,
                    options => options.MapFrom(source => source.Department!.Name))
                .ForMember(
                    destination => destination.AssigneeName,
                    options => options.MapFrom(source => source.CurrentAssignee!.FullName));
        }
    }
}
