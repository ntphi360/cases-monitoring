using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaseAssignmentsController : ControllerBase
{
    private const string AssigneeNotAuthorizedMessage =
        "Cán bộ không được phân quyền xử lý lĩnh vực của thủ tục này";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CaseAssignmentsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CaseAssignmentDto>> GetCaseAssignmentById(int id)
    {
        var assignment = await _unitOfWork.CaseAssignments.GetByIdAsync(id);
        return assignment == null
            ? NotFound()
            : Ok(_mapper.Map<CaseAssignmentDto>(assignment));
    }

    [HttpGet("by-case/{caseId:int}")]
    public async Task<ActionResult<List<CaseAssignmentDto>>> GetCaseAssignmentsByCaseId(
        int caseId)
    {
        var assignments = await _unitOfWork.CaseAssignments.GetByCaseIdAsync(caseId);
        return Ok(_mapper.Map<List<CaseAssignmentDto>>(assignments));
    }

    [HttpPost]
    public async Task<ActionResult<CaseAssignmentDto>> CreateCaseAssignment(
        [FromBody] CreateCaseAssignmentRequest request)
    {
        var caseEntity = await _unitOfWork.Cases.GetByIdAsync(request.CaseId);
        if (caseEntity == null)
        {
            return BadRequest("Hồ sơ không tồn tại");
        }

        if (!await _unitOfWork.UserProcedureFields.CanUserHandleProcedureAsync(
                request.AssignedToUserId,
                caseEntity.ProcedureId))
        {
            return BadRequest(AssigneeNotAuthorizedMessage);
        }

        var assignment = _mapper.Map<CaseAssignment>(request);
        assignment.AssignedAt = DateTime.Now;
        assignment.CreatedAt = DateTime.Now;

        _unitOfWork.CaseAssignments.Add(assignment);
        _unitOfWork.Notifications.Add(new Notification
        {
            CaseId = caseEntity.Id,
            UserId = request.AssignedToUserId,
            Message = $"Bạn được phân công xử lý hồ sơ {caseEntity.ExternalCaseCode}",
            IsRead = false,
            CreatedAt = DateTime.Now
        });
        await _unitOfWork.CompleteAsync();

        var result = _mapper.Map<CaseAssignmentDto>(assignment);
        return CreatedAtAction(
            nameof(GetCaseAssignmentById),
            new { id = assignment.Id },
            result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CaseAssignmentDto>> UpdateCaseAssignment(
        int id,
        [FromBody] UpdateCaseAssignmentRequest request)
    {
        var assignment = await _unitOfWork.CaseAssignments.GetByIdAsync(id);
        if (assignment == null)
        {
            return NotFound();
        }

        _mapper.Map(request, assignment);
        await _unitOfWork.CompleteAsync();
        return Ok(_mapper.Map<CaseAssignmentDto>(assignment));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCaseAssignment(int id)
    {
        var assignment = await _unitOfWork.CaseAssignments.GetByIdAsync(id);
        if (assignment == null)
        {
            return NotFound();
        }

        _unitOfWork.CaseAssignments.Remove(assignment);
        await _unitOfWork.CompleteAsync();
        return NoContent();
    }
}
