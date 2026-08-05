using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HoSoMonitoring.Core.Models.Content
{
    public class CreateUpdateCaseRequest
    {
        [Required]
        [MaxLength(100)]
        public string ExternalCaseCode { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ProcedureId { get; set; }

        [Range(1, int.MaxValue)]
        public int DepartmentId { get; set; }

        public DateTime ReceivedAt { get; set; }

        public DateTime Deadline { get; set; }

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