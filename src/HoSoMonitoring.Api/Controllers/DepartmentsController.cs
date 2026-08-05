using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DepartmentsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
    {
        var departments = await _unitOfWork.Departments.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<DepartmentDto>>(departments));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> GetDepartmentById(int id)
    {
        var department = await _unitOfWork.Departments.GetByIdAsync(id);
        return department == null
            ? NotFound()
            : Ok(_mapper.Map<DepartmentDto>(department));
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> CreateDepartment(
        [FromBody] CreateUpdateDepartmentRequest request)
    {
        var department = _mapper.Map<Department>(request);
        department.CreatedAt = DateTime.Now;
        _unitOfWork.Departments.Add(department);
        await _unitOfWork.CompleteAsync();

        var result = _mapper.Map<DepartmentDto>(department);
        return CreatedAtAction(nameof(GetDepartmentById), new { id = department.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> UpdateDepartment(
        int id,
        [FromBody] CreateUpdateDepartmentRequest request)
    {
        var department = await _unitOfWork.Departments.GetByIdAsync(id);
        if (department == null)
        {
            return NotFound();
        }

        _mapper.Map(request, department);
        department.UpdatedAt = DateTime.Now;
        await _unitOfWork.CompleteAsync();
        return Ok(_mapper.Map<DepartmentDto>(department));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _unitOfWork.Departments.GetByIdAsync(id);
        if (department == null)
        {
            return NotFound();
        }

        _unitOfWork.Departments.Remove(department);
        await _unitOfWork.CompleteAsync();
        return NoContent();
    }
}
