using HoSoMonitoring.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace HoSoMonitoring.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".csv" };

    private readonly IImportService _importService;

    public ImportController(IImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("cases")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportCases(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null)
        {
            return BadRequest(new { message = "Vui lòng chọn file cần import." });
        }

        if (file.Length == 0)
        {
            return BadRequest(new { message = "File import không được để trống." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!SupportedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Chỉ hỗ trợ file .xlsx và .csv." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _importService.ImportCasesAsync(
                stream,
                extension,
                file.FileName,
                cancellationToken);

            return Ok(result);
        }
        catch (ImportFileValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("last-sync")]
    public async Task<IActionResult> GetLastSync(
        CancellationToken cancellationToken)
    {
        return Ok(await _importService.GetLastSyncAsync(cancellationToken));
    }
}
