using HoSoMonitoring.Core.Models.Import;

namespace HoSoMonitoring.Core.Services;

public interface IImportService
{
    Task<ImportCasesResultDto> ImportCasesAsync(
        Stream stream,
        string fileExtension,
        CancellationToken cancellationToken = default);
}
