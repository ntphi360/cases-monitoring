using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HoSoMonitoring.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly HoSoMonitoringContext _context;

    public UsersController(UserManager<User> userManager, HoSoMonitoringContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userManager.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
        return Ok(await ToDtosAsync(users));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("management")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetManagedUsers()
    {
        var users = await _userManager.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
        return Ok(await ToDtosAsync(users));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user == null ? NotFound() : Ok(await ToDtoAsync(user));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserAccountRequest request)
    {
        if (!IsValidRole(request.Role)) return BadRequest(new { message = "Vai trò không hợp lệ." });
        if (!await DepartmentExistsAsync(request.DepartmentId)) return BadRequest(new { message = "Phòng ban không hợp lệ." });

        var user = new User
        {
            UserName = request.UserName.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = NullIfWhiteSpace(request.PhoneNumber),
            DepartmentId = request.DepartmentId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded) return IdentityError(createResult);
        var roleResult = await _userManager.AddToRoleAsync(user, NormalizeRole(request.Role));
        if (!roleResult.Succeeded) return IdentityError(roleResult);
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, await ToDtoAsync(user));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, UpdateUserAccountRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        if (!IsValidRole(request.Role)) return BadRequest(new { message = "Vai trò không hợp lệ." });
        if (!await DepartmentExistsAsync(request.DepartmentId)) return BadRequest(new { message = "Phòng ban không hợp lệ." });
        if (IsCurrentUser(id) && (!request.IsActive || NormalizeRole(request.Role) != Roles.Admin))
        {
            return BadRequest(new { message = "Không thể khóa hoặc hạ quyền tài khoản ADMIN đang đăng nhập." });
        }

        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim();
        user.PhoneNumber = NullIfWhiteSpace(request.PhoneNumber);
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.Now;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) return IdentityError(updateResult);
        var roleResult = await ReplaceRoleAsync(user, NormalizeRole(request.Role));
        if (!roleResult.Succeeded) return IdentityError(roleResult);
        if (!user.IsActive) await RevokeRefreshTokensAsync(user.Id);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(await ToDtoAsync(user));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{id:int}/active")]
    public async Task<ActionResult<UserDto>> SetActive(int id, SetUserActiveRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        if (IsCurrentUser(id) && !request.IsActive)
        {
            return BadRequest(new { message = "Không thể khóa tài khoản ADMIN đang đăng nhập." });
        }
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.Now;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return IdentityError(result);
        if (!user.IsActive)
        {
            await RevokeRefreshTokensAsync(user.Id);
            await _context.SaveChangesAsync();
        }
        return Ok(await ToDtoAsync(user));
    }

    private async Task<IdentityResult> ReplaceRoleAsync(User user, string role)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) return removeResult;
        }
        return await _userManager.AddToRoleAsync(user, role);
    }

    private Task<bool> DepartmentExistsAsync(int id) =>
        _context.Departments.AnyAsync(x => x.Id == id && x.IsActive);

    private async Task RevokeRefreshTokensAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.UserId == userId && !x.RevokedAt.HasValue && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var token in tokens) token.RevokedAt = DateTime.UtcNow;
    }

    private async Task<List<UserDto>> ToDtosAsync(IEnumerable<User> users)
    {
        var result = new List<UserDto>();
        foreach (var user in users) result.Add(await ToDtoAsync(user));
        return result;
    }

    private async Task<UserDto> ToDtoAsync(User user) => new()
    {
        Id = user.Id,
        Username = user.UserName ?? string.Empty,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        PhoneNumber = user.PhoneNumber,
        DepartmentId = user.DepartmentId,
        ExternalUserCode = user.ExternalUserCode,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Roles = (await _userManager.GetRolesAsync(user)).ToArray()
    };

    private ActionResult IdentityError(IdentityResult result) =>
        BadRequest(new { message = string.Join("; ", result.Errors.Select(x => x.Description)) });

    private bool IsCurrentUser(int id) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId) && currentId == id;

    private static bool IsValidRole(string role) =>
        NormalizeRole(role) is Roles.Admin or Roles.Staff;
    private static string NormalizeRole(string role) => role.Trim().ToUpperInvariant();
    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
