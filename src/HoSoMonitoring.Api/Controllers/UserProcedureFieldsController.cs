using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProcedureFieldsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserProcedureFieldsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet("by-user/{userId:int}")]
    public async Task<ActionResult<List<UserProcedureFieldDto>>> GetByUserId(
        int userId)
    {
        var permissions = await _unitOfWork.UserProcedureFields
            .GetByUserIdAsync(userId);
        return Ok(_mapper.Map<List<UserProcedureFieldDto>>(permissions));
    }

    [HttpGet("by-field/{procedureFieldId:int}")]
    public async Task<ActionResult<List<UserProcedureFieldDto>>> GetByFieldId(
        int procedureFieldId)
    {
        var permissions = await _unitOfWork.UserProcedureFields
            .GetByProcedureFieldIdAsync(procedureFieldId);
        return Ok(_mapper.Map<List<UserProcedureFieldDto>>(permissions));
    }

    [HttpPost]
    public async Task<ActionResult<UserProcedureFieldDto>> Create(
        [FromBody] CreateUserProcedureFieldRequest request)
    {
        if (await _unitOfWork.UserProcedureFields.ExistsAsync(
                request.UserId,
                request.ProcedureFieldId))
        {
            return BadRequest("Cán bộ đã được phân quyền xử lý lĩnh vực này");
        }

        if (await _unitOfWork.Users.GetByIdAsync(request.UserId) == null
            || await _unitOfWork.ProcedureFields.GetByIdAsync(
                request.ProcedureFieldId) == null)
        {
            return BadRequest("Cán bộ hoặc lĩnh vực thủ tục không tồn tại");
        }

        var permission = _mapper.Map<UserProcedureField>(request);
        permission.CreatedAt = DateTime.Now;

        _unitOfWork.UserProcedureFields.Add(permission);
        await _unitOfWork.CompleteAsync();

        var savedPermission = (await _unitOfWork.UserProcedureFields
                .GetByUserIdAsync(permission.UserId))
            .Single(x => x.Id == permission.Id);
        var result = _mapper.Map<UserProcedureFieldDto>(savedPermission);
        return CreatedAtAction(
            nameof(GetByUserId),
            new { userId = permission.UserId },
            result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var permission = await _unitOfWork.UserProcedureFields.GetByIdAsync(id);
        if (permission == null)
        {
            return NotFound();
        }

        _unitOfWork.UserProcedureFields.Remove(permission);
        await _unitOfWork.CompleteAsync();
        return NoContent();
    }
}
