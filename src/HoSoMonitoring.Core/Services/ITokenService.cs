using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Auth;

namespace HoSoMonitoring.Core.Services;

public interface ITokenService
{
    IssuedAccessToken CreateAccessToken(User user, IEnumerable<string> roles);
    IssuedRefreshToken CreateRefreshToken();
    string HashRefreshToken(string token);
}
