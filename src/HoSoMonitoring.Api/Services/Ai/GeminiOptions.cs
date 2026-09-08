namespace HoSoMonitoring.Api.Services.Ai;

public class GeminiOptions
{
    public string Model { get; set; } = "gemini-2.5-flash";

    public int MaxOutputTokens { get; set; } = 200;

    public double Temperature { get; set; } = 0.7;
}