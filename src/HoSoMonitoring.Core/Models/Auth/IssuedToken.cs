namespace HoSoMonitoring.Core.Models.Auth;

public record IssuedAccessToken(string Value, DateTime ExpiresAt);
public record IssuedRefreshToken(string Value, string Hash, DateTime ExpiresAt);
