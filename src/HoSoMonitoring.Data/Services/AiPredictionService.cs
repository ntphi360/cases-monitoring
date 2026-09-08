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

        _logger.LogInformation(
            "Python executable đang dùng: {PythonExecutable}",
            _options.PythonExecutable);

        _logger.LogInformation(
            "Python script đang dùng: {ScriptPath}",
            _scriptPath);

        // Quan trọng: dùng UTF-8 KHÔNG BOM để Python json.loads đọc stdin ổn định.
        var utf8WithoutBom = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false);

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.PythonExecutable.Trim(),
            WorkingDirectory = Path.GetDirectoryName(_scriptPath),

            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,

            StandardInputEncoding = utf8WithoutBom,
            StandardOutputEncoding = utf8WithoutBom,
            StandardErrorEncoding = utf8WithoutBom,

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

        // Đọc stdout/stderr song song để tránh deadlock.
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        using var timeoutSource = new CancellationTokenSource(
            TimeSpan.FromSeconds(_options.TimeoutSeconds));

        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            var requestJson = JsonSerializer.Serialize(
                request,
                _jsonOptions);

            // Debug tạm thời. Sau khi chạy ổn có thể bỏ log JSON này.
            _logger.LogInformation(
                "AI prediction request JSON: {RequestJson}",
                requestJson);

            await process.StandardInput.WriteAsync(
                requestJson.AsMemory(),
                linkedSource.Token);

            await process.StandardInput.FlushAsync(
                linkedSource.Token);

            // sys.stdin.read() bên Python chỉ kết thúc khi stdin được đóng.
            process.StandardInput.Close();

            await process.WaitForExitAsync(
                linkedSource.Token);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            KillProcess(process);

            _logger.LogInformation(
                "AI prediction đã bị caller hủy.");

            throw;
        }
        catch (OperationCanceledException)
            when (timeoutSource.IsCancellationRequested)
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

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            if (process.ExitCode == 0)
            {
                // Warning/debug trên stderr không được coi là failure.
                _logger.LogWarning(
                    "Python AI prediction stderr: {StandardError}",
                    standardError.Trim());
            }
            else
            {
                _logger.LogError(
                    "Python process cho AI prediction thất bại. Stderr: {StandardError}",
                    standardError.Trim());
            }
        }

        if (process.ExitCode != 0)
        {
            var pythonError = ReadPythonError(
                standardOutput);

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

        var reportedError = ReadPythonError(
            standardOutput);

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
            _logger.LogError(
                exception,
                "Python trả về stdout không phải JSON hợp lệ. Stdout: {StandardOutput}",
                standardOutput);

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
            return Path.GetFullPath(
                scriptPath,
                contentRootPath);
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
        if (string.IsNullOrWhiteSpace(
            _options.PythonExecutable))
        {
            throw new InvalidOperationException(
                "AiPrediction:PythonExecutable không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(
            _options.ScriptPath))
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

        // Nếu config là absolute path tới python.exe thì kiểm tra tồn tại.
        // Nếu chỉ là "python" thì để Windows PATH resolve.
        var pythonExecutable = _options.PythonExecutable.Trim();

        if (Path.IsPathRooted(pythonExecutable)
            && !File.Exists(pythonExecutable))
        {
            throw new FileNotFoundException(
                $"Không tìm thấy Python executable tại '{pythonExecutable}'.",
                pythonExecutable);
        }
    }

    private string? ReadPythonError(
        string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(
                output.Trim());

            if (
                document.RootElement.ValueKind
                    == JsonValueKind.Object
                && document.RootElement.TryGetProperty(
                    "error",
                    out var error))
            {
                return error.ValueKind
                       == JsonValueKind.String
                    ? error.GetString()
                    : error.GetRawText();
            }
        }
        catch (JsonException)
        {
            // Main deserialization path sẽ báo lỗi JSON chi tiết hơn.
        }

        return null;
    }

    private void KillProcess(
        Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(
                    entireProcessTree: true);
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
