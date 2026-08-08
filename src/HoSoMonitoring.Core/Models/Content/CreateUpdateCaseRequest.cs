using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content
{
    public class CreateUpdateCaseRequest
    {
        [MaxLength(100)]
        public string? ExternalCaseCode { get; set; }

        [Required]
        [MaxLength(250)]
        public string ApplicantName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? OrganizationName { get; set; }

        [Range(1, int.MaxValue)]
        public int ProcedureId { get; set; }

        [Range(1, int.MaxValue)]
        public int DepartmentId { get; set; }

        public DateTime ReceivedAt { get; set; }

        public DateTime? AppointmentDate { get; set; }

        public DateTime Deadline { get; set; }

        [Range(0, int.MaxValue)]
        public int? ProcessingDays { get; set; }

        public CaseStatus Status { get; set; }

        public CasePriority Priority { get; set; }

        public int? CurrentAssigneeId { get; set; }

        [MaxLength(250)]
        public string? CurrentStepName { get; set; }

        public DataSourceType SourceType { get; set; }

        public class AutoMapperProfiles : Profile
        {
            public AutoMapperProfiles()
            {
                CreateMap<CreateUpdateCaseRequest, Case>();
            }
        }
    }
}
