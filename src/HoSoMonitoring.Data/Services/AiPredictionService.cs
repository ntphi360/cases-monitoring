using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Models.AiPrediction;
using HoSoMonitoring.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HoSoMonitoring.Data.Services;

public class AiPredictionService : IAiPredictionService
{
    private readonly AiPredictionOptions _options;
    private readonly ILogger<AiPredictionService> _logger;
    private readonly string _scriptPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AiPredictionService(
        IOptions<AiPredictionOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<AiPredictionService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _scriptPath = ResolveScriptPath(
            _options.ScriptPath,
            hostEnvironment.ContentRootPath);
    }

    public async Task<AiPredictionResultDto> PredictAsync(
        AiPredictionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateConfiguration();

        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Bắt đầu AI prediction.");

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.PythonExecutable.Trim(),
            WorkingDirectory = Path.GetDirectoryName(_scriptPath),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(_scriptPath);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "Không thể khởi động Python process cho AI prediction.");
            }
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Không thể khởi động Python executable '{_options.PythonExecutable}'.",
                exception);
        }

        _logger.LogInformation(
            "Đã khởi động Python process {ProcessId} cho AI prediction.",
            process.Id);

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        using var timeoutSource = new CancellationTokenSource(
            TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            await process.StandardInput.WriteAsync(
                requestJson.AsMemory(),
                linkedSource.Token);
            await process.StandardInput.FlushAsync(linkedSource.Token);
            process.StandardInput.Close();

            await process.WaitForExitAsync(linkedSource.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            KillProcess(process);
            _logger.LogInformation("AI prediction đã bị caller hủy.");
            throw;
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            KillProcess(process);
            _logger.LogError(
                "AI prediction vượt quá timeout {TimeoutSeconds} giây.",
                _options.TimeoutSeconds);
            throw new TimeoutException(
                $"AI prediction không hoàn tất trong {_options.TimeoutSeconds} giây.");
        }
        catch
        {
            KillProcess(process);
            throw;
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        _logger.LogInformation(
            "Python process cho AI prediction đã thoát với mã {ExitCode}.",
            process.ExitCode);

        if (process.ExitCode != 0)
        {
            if (!string.IsNullOrWhiteSpace(standardError))
            {
                _logger.LogError(
                    "Python process cho AI prediction thất bại. Stderr: {StandardError}",
                    standardError.Trim());
            }

            var pythonError = ReadPythonError(standardOutput);
            var errorDetail = !string.IsNullOrWhiteSpace(pythonError)
                ? pythonError
                : standardError.Trim();
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorDetail)
                    ? $"AI prediction process thất bại với exit code {process.ExitCode}."
                    : $"AI prediction process thất bại với exit code {process.ExitCode}: {errorDetail}");
        }

        if (string.IsNullOrWhiteSpace(standardOutput))
        {
            throw new InvalidOperationException(
                "AI prediction process không trả dữ liệu qua stdout.");
        }

        var reportedError = ReadPythonError(standardOutput);
        if (!string.IsNullOrWhiteSpace(reportedError))
        {
            throw new InvalidOperationException(
                $"AI prediction thất bại: {reportedError}");
        }

        try
        {
            return JsonSerializer.Deserialize<AiPredictionResultDto>(
                       standardOutput.Trim(),
                       _jsonOptions)
                   ?? throw new InvalidOperationException(
                       "Không thể deserialize kết quả AI prediction.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Python process trả về JSON AI prediction không hợp lệ.",
                exception);
        }
    }

    private static string ResolveScriptPath(
        string scriptPath,
        string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(scriptPath, contentRootPath);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new InvalidOperationException(
                $"AiPrediction:ScriptPath '{scriptPath}' không hợp lệ.",
                exception);
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.PythonExecutable))
        {
            throw new InvalidOperationException(
                "AiPrediction:PythonExecutable không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(_options.ScriptPath))
        {
            throw new InvalidOperationException(
                "AiPrediction:ScriptPath không được để trống.");
        }

        if (_options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "AiPrediction:TimeoutSeconds phải lớn hơn 0.");
        }

        if (!File.Exists(_scriptPath))
        {
            throw new FileNotFoundException(
                $"Không tìm thấy Python script AI prediction tại '{_scriptPath}'.",
                _scriptPath);
        }
    }

    private string? ReadPythonError(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(output.Trim());
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var error))
            {
                return error.ValueKind == JsonValueKind.String
                    ? error.GetString()
                    : error.GetRawText();
            }
        }
        catch (JsonException)
        {
            // The main deserialization path reports malformed JSON.
        }

        return null;
    }

    private void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
                or Win32Exception
                or NotSupportedException)
        {
            _logger.LogError(
                exception,
                "Không thể dừng Python process {ProcessId}.",
                process.Id);
        }
    }
}
