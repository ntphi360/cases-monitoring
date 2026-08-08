namespace HoSoMonitoring.Core.Services;

public class ImportFileValidationException : Exception
{
    public ImportFileValidationException(string message)
        : base(message)
    {
    }
}
