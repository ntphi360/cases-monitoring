using HoSoMonitoring.Core.Models;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var result = await _unitOfWork.Cases.GetDashboardSummaryAsync();
        return Ok(result);
    }
}
