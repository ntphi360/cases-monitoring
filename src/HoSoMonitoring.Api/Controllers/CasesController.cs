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

        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueCases([FromQuery] int count = 10)
        {
            var cases = await _caseRepository.GetOverdueCasesAsync(count);

            return Ok(cases);
        }
    }
}