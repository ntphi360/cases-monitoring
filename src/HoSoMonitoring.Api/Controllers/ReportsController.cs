using HoSoMonitoring.Core.Enums;
using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryDto>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? procedureFieldId,
        [FromQuery] int? procedureId,
        [FromQuery] int? departmentId,
        [FromQuery] int? assignedUserId,
        [FromQuery] CaseStatus? status)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
        {
            return BadRequest(new { message = "Từ ngày không được lớn hơn đến ngày." });
        }

        var result = await _unitOfWork.Cases.GetReportSummaryAsync(
            new ReportFilterDto
            {
                From = from,
                To = to,
                ProcedureFieldId = procedureFieldId,
                ProcedureId = procedureId,
                DepartmentId = departmentId,
                AssignedUserId = assignedUserId,
                Status = status
            });

        return Ok(result);
    }
}
