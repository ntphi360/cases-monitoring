using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaseHistoriesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CaseHistoriesController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CaseHistoryDto>> GetCaseHistoryById(int id)
    {
        var history = await _unitOfWork.CaseHistories.GetByIdAsync(id);
        return history == null
            ? NotFound()
            : Ok(_mapper.Map<CaseHistoryDto>(history));
    }

    [HttpGet("by-case/{caseId:int}")]
    public async Task<ActionResult<List<CaseHistoryDto>>> GetCaseHistoriesByCaseId(
        int caseId)
    {
        var histories = await _unitOfWork.CaseHistories.GetByCaseIdAsync(caseId);
        return Ok(_mapper.Map<List<CaseHistoryDto>>(histories));
    }

    [HttpPost]
    public async Task<ActionResult<CaseHistoryDto>> CreateCaseHistory(
        [FromBody] CreateCaseHistoryRequest request)
    {
        var history = _mapper.Map<CaseHistory>(request);
        history.CreatedAt = DateTime.Now;

        _unitOfWork.CaseHistories.Add(history);
        await _unitOfWork.CompleteAsync();

        var result = _mapper.Map<CaseHistoryDto>(history);
        return CreatedAtAction(
            nameof(GetCaseHistoryById),
            new { id = history.Id },
            result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCaseHistory(int id)
    {
        var history = await _unitOfWork.CaseHistories.GetByIdAsync(id);
        if (history == null)
        {
            return NotFound();
        }

        _unitOfWork.CaseHistories.Remove(history);
        await _unitOfWork.CompleteAsync();
        return NoContent();
    }
}
