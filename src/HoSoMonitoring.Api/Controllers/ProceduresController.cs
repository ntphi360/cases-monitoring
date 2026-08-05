using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProceduresController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProceduresController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcedureDto>>> GetProcedures()
    {
        var procedures = await _unitOfWork.Procedures.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<ProcedureDto>>(procedures));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProcedureDto>> GetProcedureById(int id)
    {
        var procedure = await _unitOfWork.Procedures.GetByIdAsync(id);
        return procedure == null
            ? NotFound()
            : Ok(_mapper.Map<ProcedureDto>(procedure));
    }

    [HttpPost]
    public async Task<ActionResult<ProcedureDto>> CreateProcedure(
        [FromBody] CreateUpdateProcedureRequest request)
    {
        var procedure = _mapper.Map<Procedure>(request);
        procedure.CreatedAt = DateTime.Now;
        _unitOfWork.Procedures.Add(procedure);
        await _unitOfWork.CompleteAsync();

        var result = _mapper.Map<ProcedureDto>(procedure);
        return CreatedAtAction(nameof(GetProcedureById), new { id = procedure.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProcedureDto>> UpdateProcedure(
        int id,
        [FromBody] CreateUpdateProcedureRequest request)
    {
        var procedure = await _unitOfWork.Procedures.GetByIdAsync(id);
        if (procedure == null)
        {
            return NotFound();
        }

        _mapper.Map(request, procedure);
        procedure.UpdatedAt = DateTime.Now;
        await _unitOfWork.CompleteAsync();
        return Ok(_mapper.Map<ProcedureDto>(procedure));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProcedure(int id)
    {
        var procedure = await _unitOfWork.Procedures.GetByIdAsync(id);
        if (procedure == null)
        {
            return NotFound();
        }

        _unitOfWork.Procedures.Remove(procedure);
        await _unitOfWork.CompleteAsync();
        return NoContent();
    }
}
