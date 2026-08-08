using HoSoMonitoring.Core.Models.Content;

namespace HoSoMonitoring.Core.Services;

public interface ICaseCodeParser
{
    CaseCodeInfo Parse(string? externalCaseCode);
}
