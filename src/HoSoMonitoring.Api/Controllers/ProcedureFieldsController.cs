using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcedureFieldsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProcedureFieldsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcedureFieldDto>>> GetProcedureFields()
    {
        var fields = await _unitOfWork.ProcedureFields.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<ProcedureFieldDto>>(fields));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProcedureFieldDto>> GetProcedureFieldById(int id)
    {
        var field = await _unitOfWork.ProcedureFields.GetByIdAsync(id);
        return field == null
            ? NotFound()
            : Ok(_mapper.Map<ProcedureFieldDto>(field));
    }

    [HttpPost]
    public async Task<ActionResult<ProcedureFieldDto>> CreateProcedureField(
        [FromBody] CreateUpdateProcedureFieldRequest request)
    {
        var field = _mapper.Map<ProcedureField>(request);
        _unitOfWork.ProcedureFields.Add(field);
        await _unitOfWork.CompleteAsync();

        var result = _mapper.Map<ProcedureFieldDto>(field);
        return CreatedAtAction(nameof(GetProcedureFieldById), new { id = field.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProcedureFieldDto>> UpdateProcedureField(
        int id,
        [FromBody] CreateUpdateProcedureFieldRequest request)
    {
        var field = await _unitOfWork.ProcedureFields.GetByIdAsync(id);
        if (field == null)
        {
            return NotFound();
        }

        _mapper.Map(request, field);
        await _unitOfWork.CompleteAsync();
        return Ok(_mapper.Map<ProcedureFieldDto>(field));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProcedureField(int id)
    {
        var field = await _unitOfWork.ProcedureFields.GetByIdAsync(id);
        if (field == null)
        {
            return NotFound();
        }

        _unitOfWork.ProcedureFields.Remove(field);
        await _unitOfWork.CompleteAsync();
        return NoContent();
    }
}
