using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Auth;
using HoSoMonitoring.Core.Services;
using HoSoMonitoring.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "hoso_refresh";
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly HoSoMonitoringContext _context;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ITokenService tokenService,
        HoSoMonitoringContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthEnvelope>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName.Trim());
        if (user == null || !user.IsActive
            || !(await _signInManager.CheckPasswordSignInAsync(user, request.Password, true)).Succeeded)
        {
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });
        }

        return Ok(new AuthEnvelope { Data = await IssueSessionAsync(user) });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthEnvelope>> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var rawToken))
        {
            return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn." });
        }

        var hash = _tokenService.HashRefreshToken(rawToken);
        var storedToken = await _context.RefreshTokens
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == hash);
        if (storedToken?.User == null || storedToken.RevokedAt.HasValue
            || storedToken.ExpiresAt <= DateTime.UtcNow || !storedToken.User.IsActive)
        {
            DeleteRefreshCookie();
            return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn." });
        }

        var nextToken = _tokenService.CreateRefreshToken();
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = nextToken.Hash;
        _context.RefreshTokens.Add(CreateRefreshToken(storedToken.UserId, nextToken));
        await _context.SaveChangesAsync();
        SetRefreshCookie(nextToken);
        return Ok(new AuthEnvelope { Data = await BuildAuthResponseAsync(storedToken.User) });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var rawToken))
        {
            var hash = _tokenService.HashRefreshToken(rawToken);
            var storedToken = await _context.RefreshTokens.SingleOrDefaultAsync(item => item.TokenHash == hash);
            if (storedToken != null && !storedToken.RevokedAt.HasValue)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        DeleteRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(id, out var userId)) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive) return Unauthorized();
        return Ok(await ToDtoAsync(user));
    }

    private async Task<AuthResponse> IssueSessionAsync(User user)
    {
        var refreshToken = _tokenService.CreateRefreshToken();
        _context.RefreshTokens.Add(CreateRefreshToken(user.Id, refreshToken));
        await _context.SaveChangesAsync();
        SetRefreshCookie(refreshToken);
        return await BuildAuthResponseAsync(user);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        return new AuthResponse
        {
            AccessToken = accessToken.Value,
            ExpiresAt = accessToken.ExpiresAt,
            User = ToDto(user, roles)
        };
    }

    private async Task<AuthUserDto> ToDtoAsync(User user) =>
        ToDto(user, await _userManager.GetRolesAsync(user));

    private static AuthUserDto ToDto(User user, IEnumerable<string> roles) => new()
    {
        Id = user.Id,
        UserName = user.UserName ?? string.Empty,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        Roles = roles.ToArray(),
        IsActive = user.IsActive
    };

    private static RefreshToken CreateRefreshToken(int userId, IssuedRefreshToken token) => new()
    {
        UserId = userId,
        TokenHash = token.Hash,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = token.ExpiresAt
    };

    private void SetRefreshCookie(IssuedRefreshToken token) =>
        Response.Cookies.Append(RefreshCookieName, token.Value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth",
            Expires = token.ExpiresAt
        });

    private void DeleteRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth"
        });
}
