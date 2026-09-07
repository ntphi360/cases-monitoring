namespace HoSoMonitoring.Core.Models.AiPrediction;

public class AiPredictionWorkloadDto
{
    public int OfficerCasesBeforeToday { get; set; }

    public int DepartmentCasesBeforeToday { get; set; }

    public int ProcedureCasesBeforeToday { get; set; }

    public int OfficerCasesBeforeAll { get; set; }

    public int DepartmentCasesBeforeAll { get; set; }

    public int ProcedureCasesBeforeAll { get; set; }

    public int OfficerCasesPrevious7Days { get; set; }

    public int DepartmentCasesPrevious7Days { get; set; }

    public int ProcedureCasesPrevious7Days { get; set; }
}
