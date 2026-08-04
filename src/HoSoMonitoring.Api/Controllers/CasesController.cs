using HoSoMonitoring.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CasesController : ControllerBase
    {
        private readonly ICaseRepository _caseRepository;

        public CasesController(ICaseRepository caseRepository)
        {
            _caseRepository = caseRepository;
        }

        // GET /api/cases/overdue?count=10
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueCases(
            [FromQuery] int count = 10)
        {
            var cases = await _caseRepository
                .GetOverdueCasesAsync(count);

            return Ok(cases);
        }

        // GET /api/cases?pageIndex=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetCases(
            [FromQuery] string? keyword,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _caseRepository
                .GetAllPagingAsync(
                    keyword,
                    pageIndex,
                    pageSize);

            return Ok(result);
        }
    }
}