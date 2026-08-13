using ClosedXML.Excel;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Enums;
using HoSoMonitoring.Core.Models.Import;
using HoSoMonitoring.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HoSoMonitoring.Data.Services;

public class ImportService : IImportService
{
    private const string ExternalCaseCodeHeader = "Số hồ sơ";
    private const string ProcedureNameHeader = "Tên thủ tục hành chính";
    private const string ProcedureFieldNameHeader = "Tên lĩnh vực";
    private const string DepartmentNameHeader = "Phòng ban";
    private const string ApplicantNameHeader = "Chủ hồ sơ";
    private const string PhoneNumberHeader = "Số điện thoại";
    private const string OrganizationNameHeader = "Cơ quan/đơn vị";
    private const string ReceivedAtHeader = "Ngày tiếp nhận";
    private const string AppointmentDateHeader = "Ngày hẹn trả";
    private const string CompletedAtHeader = "Ngày kết thúc xử lý";
    private const string ProcessingDaysHeader = "Số ngày giải quyết";
    private const string AssigneeNameHeader = "Cán bộ xử lý hiện tại";
    private const string StatusHeader = "Trạng thái";
    private const string DeadlineHeader = "Hạn xử lý";
    private const string CurrentStepNameHeader = "Bước xử lý hiện tại";
    private const string ExternalUpdatedAtHeader = "Cập nhật nguồn";

    private static readonly string[] RequiredHeaders =
    [
        ExternalCaseCodeHeader,
        ProcedureNameHeader,
        ApplicantNameHeader,
        ReceivedAtHeader
    ];

    private static readonly string[] SupportedDateFormats =
    [
        "dd/MM/yyyy",
        "dd/MM/yyyy HH:mm"
    ];

    private readonly HoSoMonitoringContext _context;
    private readonly MonitoringOptions _monitoring;

    public ImportService(
        HoSoMonitoringContext context,
        MonitoringOptions monitoring)
    {
        _context = context;
        _monitoring = monitoring;
    }

    public async Task<ImportCasesResultDto> ImportCasesAsync(
        Stream stream,
        string fileExtension,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var importHistory = new ImportHistory
        {
            FileName = fileName.Length > 260 ? fileName[..260] : fileName,
            StartedAt = DateTime.Now
        };
        _context.ImportHistories.Add(importHistory);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await ImportRowsAsync(
                stream,
                fileExtension,
                cancellationToken);

            importHistory.CompletedAt = DateTime.Now;
            importHistory.TotalRows = result.TotalRows;
            importHistory.InsertedCount = result.InsertedCount;
            importHistory.UpdatedCount = result.UpdatedCount;
            importHistory.UnchangedCount = result.UnchangedCount;
            importHistory.FailedCount = result.FailedCount;
            importHistory.IsSuccess = true;
            await _context.SaveChangesAsync(cancellationToken);

            return result;
        }
        catch
        {
            foreach (var entry in _context.ChangeTracker.Entries()
                .Where(entry => !ReferenceEquals(entry.Entity, importHistory)))
            {
                if (entry.State == EntityState.Added)
                {
                    entry.State = EntityState.Detached;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                }
            }

            importHistory.CompletedAt = DateTime.Now;
            importHistory.IsSuccess = false;
            await _context.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<LastImportSyncDto> GetLastSyncAsync(
        CancellationToken cancellationToken = default)
    {
        var lastImport = await _context.ImportHistories
            .AsNoTracking()
            .Where(item => item.IsSuccess && item.CompletedAt.HasValue)
            .OrderByDescending(item => item.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var staleHours = _monitoring.StaleDataHours;
        var lastUpdatedAt = lastImport?.CompletedAt;

        return new LastImportSyncDto
        {
            LastUpdatedAt = lastUpdatedAt,
            FileName = lastImport?.FileName,
            IsStale = !lastUpdatedAt.HasValue
                || DateTime.Now - lastUpdatedAt.Value > TimeSpan.FromHours(staleHours),
            StaleDataHours = staleHours
        };
    }

    private async Task<ImportCasesResultDto> ImportRowsAsync(
        Stream stream,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        var rows = fileExtension.ToLowerInvariant() switch
        {
            ".xlsx" => ReadXlsxRows(stream),
            ".csv" => await ReadCsvRowsAsync(stream, cancellationToken),
            _ => throw new ImportFileValidationException("Chỉ hỗ trợ file .xlsx và .csv.")
        };

        var procedureRecords = await _context.Procedures
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.DepartmentId,
                x.DefaultProcessingHours,
                ProcedureFieldName = x.ProcedureField!.Name,
                DepartmentName = x.Department!.Name
            })
            .ToListAsync(cancellationToken);

        var procedures = procedureRecords
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => NormalizeText(x.Name))
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.Ordinal);

        var userRecords = await _context.Users
            .AsNoTracking()
            .Select(x => new { x.Id, x.FullName })
            .ToListAsync(cancellationToken);

        var users = userRecords
            .Where(x => !string.IsNullOrWhiteSpace(x.FullName))
            .GroupBy(x => NormalizeText(x.FullName))
            .ToDictionary(x => x.Key, x => x.First().Id, StringComparer.Ordinal);

        var existingCases = (await _context.Cases
                .ToListAsync(cancellationToken))
            .GroupBy(item => NormalizeText(item.ExternalCaseCode))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var errors = new List<ImportCaseErrorDto>();
        var insertedCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;

        foreach (var row in rows)
        {
            var externalCaseCode = row.GetText(ExternalCaseCodeHeader).Trim();

            if (string.IsNullOrWhiteSpace(externalCaseCode))
            {
                AddError(errors, row.Number, null, "Mã hồ sơ không được để trống");
                continue;
            }

            if (externalCaseCode.Length > 100)
            {
                AddError(errors, row.Number, externalCaseCode, "Mã hồ sơ không được vượt quá 100 ký tự");
                continue;
            }

            var normalizedCode = NormalizeText(externalCaseCode);
            existingCases.TryGetValue(normalizedCode, out var existingCase);

            var applicantName = row.GetText(ApplicantNameHeader).Trim();
            if (string.IsNullOrWhiteSpace(applicantName))
            {
                AddError(errors, row.Number, externalCaseCode, "Họ tên người nộp không được để trống");
                continue;
            }

            var organizationName = row.GetText(OrganizationNameHeader).Trim();
            if (organizationName.Length > 250)
            {
                AddError(errors, row.Number, externalCaseCode, "Cơ quan/đơn vị không được vượt quá 250 ký tự");
                continue;
            }

            if (applicantName.Length > 250)
            {
                AddError(errors, row.Number, externalCaseCode, "Họ tên người nộp không được vượt quá 250 ký tự");
                continue;
            }

            var procedureName = row.GetText(ProcedureNameHeader);
            if (!procedures.TryGetValue(
                    NormalizeText(procedureName),
                    out var procedureCandidates))
            {
                AddError(errors, row.Number, externalCaseCode, "Không tìm thấy thủ tục hành chính");
                continue;
            }

            var procedureFieldName = row.GetText(ProcedureFieldNameHeader);
            var departmentName = row.GetText(DepartmentNameHeader);
            var procedure = procedureCandidates.FirstOrDefault(candidate =>
                (string.IsNullOrWhiteSpace(procedureFieldName)
                    || NormalizeText(candidate.ProcedureFieldName)
                        == NormalizeText(procedureFieldName))
                && (string.IsNullOrWhiteSpace(departmentName)
                    || NormalizeText(candidate.DepartmentName)
                        == NormalizeText(departmentName)));

            if (procedure == null)
            {
                AddError(
                    errors,
                    row.Number,
                    externalCaseCode,
                    "Thủ tục hành chính không khớp với lĩnh vực hoặc phòng ban");
                continue;
            }

            if (!TryParseRequiredDate(row.GetCell(ReceivedAtHeader), out var receivedAt))
            {
                AddError(errors, row.Number, externalCaseCode, "Ngày nhận không hợp lệ hoặc bị thiếu");
                continue;
            }

            var appointmentDate = existingCase?.AppointmentDate;
            if (row.HasColumn(AppointmentDateHeader)
                && !TryParseOptionalDate(
                    row.GetCell(AppointmentDateHeader), out appointmentDate))
            {
                AddError(errors, row.Number, externalCaseCode, "Ngày hẹn trả không hợp lệ");
                continue;
            }

            var completedAt = existingCase?.CompletedAt;
            if (row.HasColumn(CompletedAtHeader)
                && !TryParseOptionalDate(
                    row.GetCell(CompletedAtHeader), out completedAt))
            {
                AddError(errors, row.Number, externalCaseCode, "Ngày hoàn tất không hợp lệ");
                continue;
            }

            DateTime? importedDeadline = null;
            if (row.HasColumn(DeadlineHeader)
                && !TryParseOptionalDate(row.GetCell(DeadlineHeader), out importedDeadline))
            {
                AddError(errors, row.Number, externalCaseCode, "Hạn xử lý không hợp lệ");
                continue;
            }

            DateTime? externalUpdatedAt = null;
            if (row.HasColumn(ExternalUpdatedAtHeader)
                && !TryParseOptionalDate(
                    row.GetCell(ExternalUpdatedAtHeader), out externalUpdatedAt))
            {
                AddError(errors, row.Number, externalCaseCode, "Thời điểm cập nhật nguồn không hợp lệ");
                continue;
            }

            var processingDays = existingCase?.ProcessingDays;
            if (row.HasColumn(ProcessingDaysHeader)
                && !TryParseOptionalNonNegativeInteger(
                    row.GetText(ProcessingDaysHeader),
                    out processingDays))
            {
                AddError(errors, row.Number, externalCaseCode, "Số ngày giải quyết không hợp lệ");
                continue;
            }

            int? currentAssigneeId = existingCase?.CurrentAssigneeId;
            var assigneeName = row.GetText(AssigneeNameHeader);
            if (row.HasColumn(AssigneeNameHeader))
            {
                currentAssigneeId = null;
                if (!string.IsNullOrWhiteSpace(assigneeName)
                    && users.TryGetValue(NormalizeText(assigneeName), out var userId))
                {
                    currentAssigneeId = userId;
                }
            }

            var statusText = row.GetText(StatusHeader);
            var currentStepName = row.GetText(CurrentStepNameHeader).Trim();
            if (currentStepName.Length > 250)
            {
                AddError(errors, row.Number, externalCaseCode, "Bước xử lý hiện tại không được vượt quá 250 ký tự");
                continue;
            }

            var caseStatus = existingCase?.Status ?? CaseStatus.Received;
            if (row.HasColumn(StatusHeader)
                && !string.IsNullOrWhiteSpace(statusText)
                && !TryParseCaseStatus(statusText, out caseStatus))
            {
                AddError(errors, row.Number, externalCaseCode, "Trạng thái không hợp lệ");
                continue;
            }

            if (existingCase == null)
            {
                var newCase = new Case
                {
                    ExternalCaseCode = externalCaseCode,
                    ApplicantName = applicantName,
                    OrganizationName = string.IsNullOrWhiteSpace(organizationName) ? null : organizationName,
                    ProcedureId = procedure.Id,
                    DepartmentId = procedure.DepartmentId,
                    ReceivedAt = receivedAt,
                    AppointmentDate = appointmentDate,
                    Deadline = importedDeadline ?? receivedAt.AddHours(procedure.DefaultProcessingHours),
                    CompletedAt = completedAt,
                    ProcessingDays = processingDays,
                    Status = caseStatus,
                    Priority = CasePriority.Normal,
                    CurrentAssigneeId = currentAssigneeId,
                    CurrentStepName = row.HasColumn(CurrentStepNameHeader)
                        ? NullIfWhiteSpace(currentStepName)
                        : "Tiếp nhận",
                    SourceType = DataSourceType.ManualImport,
                    ExternalUpdatedAt = externalUpdatedAt,
                    CreatedAt = DateTime.Now
                };
                _context.Cases.Add(newCase);
                existingCases[normalizedCode] = newCase;
                insertedCount++;
                continue;
            }

            var oldStatus = existingCase.Status;
            var changed = false;
            changed |= SetIfChanged(existingCase.ApplicantName, applicantName, value => existingCase.ApplicantName = value);
            changed |= SetIfChanged(existingCase.ProcedureId, procedure.Id, value => existingCase.ProcedureId = value);
            changed |= SetIfChanged(existingCase.DepartmentId, procedure.DepartmentId, value => existingCase.DepartmentId = value);
            changed |= SetIfChanged(existingCase.ReceivedAt, receivedAt, value => existingCase.ReceivedAt = value);
            changed |= SetIfChanged(existingCase.Status, caseStatus, value => existingCase.Status = value);
            changed |= SetIfChanged(existingCase.SourceType, DataSourceType.ManualImport, value => existingCase.SourceType = value);

            if (row.HasColumn(OrganizationNameHeader))
                changed |= SetIfChanged(existingCase.OrganizationName, NullIfWhiteSpace(organizationName), value => existingCase.OrganizationName = value);
            if (row.HasColumn(AppointmentDateHeader))
                changed |= SetIfChanged(existingCase.AppointmentDate, appointmentDate, value => existingCase.AppointmentDate = value);
            if (row.HasColumn(DeadlineHeader) && importedDeadline.HasValue)
                changed |= SetIfChanged(existingCase.Deadline, importedDeadline.Value, value => existingCase.Deadline = value);
            if (row.HasColumn(CompletedAtHeader))
                changed |= SetIfChanged(existingCase.CompletedAt, completedAt, value => existingCase.CompletedAt = value);
            if (row.HasColumn(ProcessingDaysHeader))
                changed |= SetIfChanged(existingCase.ProcessingDays, processingDays, value => existingCase.ProcessingDays = value);
            if (row.HasColumn(AssigneeNameHeader))
                changed |= SetIfChanged(existingCase.CurrentAssigneeId, currentAssigneeId, value => existingCase.CurrentAssigneeId = value);
            if (row.HasColumn(CurrentStepNameHeader))
                changed |= SetIfChanged(existingCase.CurrentStepName, NullIfWhiteSpace(currentStepName), value => existingCase.CurrentStepName = value);
            if (row.HasColumn(ExternalUpdatedAtHeader))
                changed |= SetIfChanged(existingCase.ExternalUpdatedAt, externalUpdatedAt, value => existingCase.ExternalUpdatedAt = value);

            if (!changed)
            {
                unchangedCount++;
                continue;
            }

            existingCase.UpdatedAt = DateTime.Now;
            updatedCount++;
            if (oldStatus != existingCase.Status)
            {
                _context.CaseHistories.Add(new CaseHistory
                {
                    Case = existingCase,
                    ActionType = CaseActionType.StatusChanged,
                    OldStatus = oldStatus,
                    NewStatus = existingCase.Status,
                    Description = "Cập nhật trạng thái từ dữ liệu nguồn",
                    CreatedAt = DateTime.Now
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ImportCasesResultDto
        {
            TotalRows = rows.Count,
            InsertedCount = insertedCount,
            UpdatedCount = updatedCount,
            UnchangedCount = unchangedCount,
            FailedCount = errors.Count,
            Errors = errors
        };
    }

    private static List<ImportRow> ReadXlsxRows(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new ImportFileValidationException("File Excel không có worksheet.");
            var headerRow = worksheet.FirstRowUsed()
                ?? throw new ImportFileValidationException("File Excel không có dữ liệu.");
            var lastHeaderCell = headerRow.LastCellUsed()
                ?? throw new ImportFileValidationException("File Excel không có header.");

            var headers = Enumerable.Range(1, lastHeaderCell.Address.ColumnNumber)
                .Select(column => headerRow.Cell(column).GetString());
            var headerMap = BuildHeaderMap(headers, startingColumnIndex: 1);
            var lastRowNumber = worksheet.LastRowUsed()?.RowNumber()
                ?? headerRow.RowNumber();
            var rows = new List<ImportRow>();

            for (var rowNumber = headerRow.RowNumber() + 1;
                 rowNumber <= lastRowNumber;
                 rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                var values = headerMap.ToDictionary(
                    x => x.Key,
                    x => ReadXlsxCell(row.Cell(x.Value)),
                    StringComparer.Ordinal);

                if (values.Values.All(x => x.IsEmpty))
                {
                    continue;
                }

                rows.Add(new ImportRow(rowNumber, values));
            }

            return rows;
        }
        catch (ImportFileValidationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ImportFileValidationException(
                $"Không thể đọc file Excel: {exception.Message}");
        }
    }

    private static async Task<List<ImportRow>> ReadCsvRowsAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true);
            var content = await reader.ReadToEndAsync(cancellationToken);
            var delimiter = DetectCsvDelimiter(content);
            var records = ParseCsv(content, delimiter);

            if (records.Count == 0)
            {
                throw new ImportFileValidationException("File CSV không có dữ liệu.");
            }

            var headerMap = BuildHeaderMap(records[0]);
            var rows = new List<ImportRow>();

            for (var index = 1; index < records.Count; index++)
            {
                var record = records[index];
                var values = headerMap.ToDictionary(
                    x => x.Key,
                    x => new ImportCell(
                        x.Value < record.Count ? record[x.Value] : string.Empty,
                        null),
                    StringComparer.Ordinal);

                if (values.Values.All(x => x.IsEmpty))
                {
                    continue;
                }

                rows.Add(new ImportRow(index + 1, values));
            }

            return rows;
        }
        catch (ImportFileValidationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ImportFileValidationException(
                $"Không thể đọc file CSV: {exception.Message}");
        }
    }

    private static Dictionary<string, int> BuildHeaderMap(
        IEnumerable<string> headers,
        int startingColumnIndex = 0)
    {
        var headerMap = new Dictionary<string, int>(StringComparer.Ordinal);
        var columnIndex = startingColumnIndex;

        foreach (var header in headers)
        {
            var normalizedHeader = NormalizeText(header.TrimStart('\uFEFF'));
            if (!string.IsNullOrEmpty(normalizedHeader))
            {
                headerMap.TryAdd(normalizedHeader, columnIndex);
            }

            columnIndex++;
        }

        AddHeaderAlias(headerMap, ExternalCaseCodeHeader, "Mã hồ sơ");
        AddHeaderAlias(headerMap, ProcedureNameHeader, "Tên TTHC");
        AddHeaderAlias(headerMap, ApplicantNameHeader, "Họ tên");
        AddHeaderAlias(headerMap, ReceivedAtHeader, "Ngày nhận");
        AddHeaderAlias(headerMap, CompletedAtHeader, "Ngày hoàn tất");
        AddHeaderAlias(headerMap, AssigneeNameHeader, "Người xử lý");

        var missingHeaders = RequiredHeaders
            .Where(header => !headerMap.ContainsKey(NormalizeText(header)))
            .ToList();

        if (missingHeaders.Count > 0)
        {
            throw new ImportFileValidationException(
                $"Thiếu header bắt buộc: {string.Join(", ", missingHeaders)}.");
        }

        return headerMap;
    }

    private static void AddHeaderAlias(
        IDictionary<string, int> headerMap,
        string canonicalHeader,
        params string[] aliases)
    {
        var canonicalKey = NormalizeText(canonicalHeader);
        if (headerMap.ContainsKey(canonicalKey))
        {
            return;
        }

        foreach (var alias in aliases)
        {
            if (headerMap.TryGetValue(NormalizeText(alias), out var columnIndex))
            {
                headerMap[canonicalKey] = columnIndex;
                return;
            }
        }
    }

    private static ImportCell ReadXlsxCell(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return ImportCell.Empty;
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            return new ImportCell(cell.GetFormattedString(), cell.GetDateTime());
        }

        return new ImportCell(cell.GetString(), null);
    }

    private static bool TryParseRequiredDate(ImportCell cell, out DateTime value)
    {
        if (cell.DateValue.HasValue)
        {
            value = cell.DateValue.Value;
            return true;
        }

        return DateTime.TryParseExact(
            cell.Text.Trim(),
            SupportedDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out value);
    }

    private static bool TryParseOptionalDate(ImportCell cell, out DateTime? value)
    {
        if (cell.IsEmpty)
        {
            value = null;
            return true;
        }

        if (TryParseRequiredDate(cell, out var parsedValue))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryParseOptionalNonNegativeInteger(
        string text,
        out int? value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }

        if (int.TryParse(
                text.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedValue)
            && parsedValue >= 0)
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryParseCaseStatus(
        string text,
        out CaseStatus status)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            status = CaseStatus.Received;
            return true;
        }

        status = NormalizeText(text) switch
        {
            "TIẾP NHẬN" or "MỚI TIẾP NHẬN" => CaseStatus.Received,
            "ĐANG XỬ LÝ" or "ĐANG GIẢI QUYẾT" => CaseStatus.InProgress,
            "CHỜ XỬ LÝ" or "TẠM DỪNG" => CaseStatus.Pending,
            "HOÀN THÀNH" or "ĐÃ HOÀN THÀNH" => CaseStatus.Completed,
            "QUÁ HẠN" => CaseStatus.Overdue,
            "HỦY" or "ĐÃ HỦY" => CaseStatus.Cancelled,
            _ => 0
        };

        return status != 0;
    }

    private static char DetectCsvDelimiter(string content)
    {
        var commaCount = 0;
        var semicolonCount = 0;
        var insideQuotes = false;

        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (character == '"')
            {
                if (insideQuotes
                    && index + 1 < content.Length
                    && content[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                insideQuotes = !insideQuotes;
            }
            else if (!insideQuotes && character is '\r' or '\n')
            {
                break;
            }
            else if (!insideQuotes && character == ',')
            {
                commaCount++;
            }
            else if (!insideQuotes && character == ';')
            {
                semicolonCount++;
            }
        }

        return semicolonCount > commaCount ? ';' : ',';
    }

    private static List<List<string>> ParseCsv(string content, char delimiter)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];

            if (character == '"')
            {
                if (insideQuotes
                    && index + 1 < content.Length
                    && content[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (!insideQuotes && character == delimiter)
            {
                record.Add(field.ToString());
                field.Clear();
                continue;
            }

            if (!insideQuotes && character is '\r' or '\n')
            {
                record.Add(field.ToString());
                field.Clear();
                records.Add(record);
                record = [];

                if (character == '\r'
                    && index + 1 < content.Length
                    && content[index + 1] == '\n')
                {
                    index++;
                }

                continue;
            }

            field.Append(character);
        }

        if (insideQuotes)
        {
            throw new ImportFileValidationException("File CSV có dấu nháy kép không hợp lệ.");
        }

        if (field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }

        return records;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC).Trim();
        return Regex.Replace(normalized, @"\s+", " ").ToUpperInvariant();
    }

    private static void AddError(
        ICollection<ImportCaseErrorDto> errors,
        int row,
        string? externalCaseCode,
        string message)
    {
        errors.Add(new ImportCaseErrorDto
        {
            Row = row,
            ExternalCaseCode = externalCaseCode,
            Message = message
        });
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool SetIfChanged<T>(
        T currentValue,
        T newValue,
        Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return false;
        }

        setter(newValue);
        return true;
    }

    private readonly record struct ImportCell(string Text, DateTime? DateValue)
    {
        public static ImportCell Empty => new(string.Empty, null);

        public bool IsEmpty => !DateValue.HasValue && string.IsNullOrWhiteSpace(Text);
    }

    private sealed record ImportRow(
        int Number,
        IReadOnlyDictionary<string, ImportCell> Values)
    {
        public ImportCell GetCell(string header)
        {
            return Values.TryGetValue(NormalizeText(header), out var value)
                ? value
                : ImportCell.Empty;
        }

        public string GetText(string header) => GetCell(header).Text;

        public bool HasColumn(string header) =>
            Values.ContainsKey(NormalizeText(header));
    }
}
