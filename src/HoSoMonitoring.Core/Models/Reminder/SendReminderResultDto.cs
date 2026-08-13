namespace HoSoMonitoring.Core.Models.Reminder;

public class SendReminderResultDto
{
    public bool Success { get; set; }

    public Dictionary<string, ReminderChannelResultDto> Data { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public class ReminderChannelResultDto
{
    public bool Success { get; set; }

    public string? Message { get; set; }
}

public class ChannelSendResult
{
    public bool Success { get; set; }

    public string? Message { get; set; }
}
