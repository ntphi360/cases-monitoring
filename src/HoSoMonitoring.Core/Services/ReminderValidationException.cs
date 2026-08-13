namespace HoSoMonitoring.Core.Services;

public class ReminderValidationException : Exception
{
    public ReminderValidationException(string message)
        : base(message)
    {
    }
}
