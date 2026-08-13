namespace HoSoMonitoring.Core.Configurations;

public class ZaloOptions
{
    public const string SectionName = "Zalo";

    public string Endpoint { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;

    public bool IsConfigured =>
        Uri.TryCreate(Endpoint, UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(AccessToken);
}
