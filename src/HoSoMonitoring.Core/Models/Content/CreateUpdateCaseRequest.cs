using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models.Content
{
    public class CreateUpdateCaseRequest
    {
        public string ExternalCaseCode { get; set; } = string.Empty;

        public int ProcedureId { get; set; }

        public int DepartmentId { get; set; }

        public DateTime ReceivedAt { get; set; }

        public DateTime Deadline { get; set; }

        public CaseStatus Status { get; set; }

        public CasePriority Priority { get; set; }

        public int? CurrentAssigneeId { get; set; }

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