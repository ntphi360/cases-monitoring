using HoSoMonitoring.Core.Enums;

namespace HoSoMonitoring.Core.Models.Content;

public class CaseExportDto
{
    public string ExternalCaseCode { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string? ProcedureFieldName { get; set; }
    public string? ProcedureName { get; set; }
    public string? DepartmentName { get; set; }
    public string? OrganizationName { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public CaseStatus Status { get; set; }
}
