namespace HoSoMonitoring.Core.Services;

public interface ICaseCodeGenerator
{
    Task<string> GenerateAsync(
        DateTime? generatedAt = null,
        CancellationToken cancellationToken = default);
}
