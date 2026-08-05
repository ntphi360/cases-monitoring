using AutoMapper;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.SeedWorks;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UsersController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        return user == null
            ? NotFound()
            : Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUpdateUserRequest request)
    {
        var user = _mapper.Map<User>(request);
        user.CreatedAt = DateTime.Now;
        _unitOfWork.Users.Add(user);
        await _unitOfWork.CompleteAsync();

        var result = _mapper.Map<UserDto>(user);
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> UpdateUser(
        int id,
        [FromBody] CreateUpdateUserRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _mapper.Map(request, user);
        user.UpdatedAt = DateTime.Now;
        await _unitOfWork.CompleteAsync();
        return Ok(_mapper.Map<UserDto>(user));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _unitOfWork.Users.Remove(user);
        await _unitOfWork.CompleteAsync();
        return NoContent();
    }
}
