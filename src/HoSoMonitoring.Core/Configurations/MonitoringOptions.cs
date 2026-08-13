namespace HoSoMonitoring.Core.Configurations;

public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int StaleDataHours { get; set; } = 24;

    public int WarningThresholdDays { get; set; } = 14;
}
