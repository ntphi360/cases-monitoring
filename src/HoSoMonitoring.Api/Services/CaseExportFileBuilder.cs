using HoSoMonitoring.Core.Enums;
using HoSoMonitoring.Core.Models.Content;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace HoSoMonitoring.Api.Services;

public static class CaseExportFileBuilder
{
    private static readonly string[] Headers =
    [
        "Mã hồ sơ",
        "Tên hồ sơ / Chủ hồ sơ",
        "Lĩnh vực",
        "Thủ tục hành chính",
        "Phòng ban",
        "Cơ quan/đơn vị",
        "Người xử lý",
        "Ngày tiếp nhận",
        "Hạn xử lý",
        "Ngày hẹn trả",
        "Ngày hoàn tất",
        "Trạng thái"
    ];

    public static byte[] BuildCsv(IEnumerable<CaseExportDto> cases)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Headers.Select(EscapeCsv)));
        foreach (var item in cases)
        {
            builder.AppendLine(string.Join(',', GetValues(item).Select(EscapeCsv)));
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetPreamble()
            .Concat(encoding.GetBytes(builder.ToString()))
            .ToArray();
    }

    public static byte[] BuildXlsx(IEnumerable<CaseExportDto> cases)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteTextEntry(archive, "[Content_Types].xml", ContentTypesXml);
            WriteTextEntry(archive, "_rels/.rels", RootRelationshipsXml);
            WriteTextEntry(archive, "xl/workbook.xml", WorkbookXml);
            WriteTextEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml);
            WriteTextEntry(archive, "xl/styles.xml", StylesXml);

            var sheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml");
            using var stream = sheetEntry.Open();
            using var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false
            });

            writer.WriteStartDocument();
            writer.WriteStartElement("worksheet", SpreadsheetNamespace);
            writer.WriteStartElement("cols");
            for (var index = 1; index <= Headers.Length; index++)
            {
                writer.WriteStartElement("col");
                writer.WriteAttributeString("min", index.ToString());
                writer.WriteAttributeString("max", index.ToString());
                writer.WriteAttributeString("width", index is 2 or 4 ? "36" : "22");
                writer.WriteAttributeString("customWidth", "1");
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteStartElement("sheetData");
            WriteRow(writer, 1, Headers, header: true);

            var rowNumber = 2;
            foreach (var item in cases)
            {
                WriteRow(writer, rowNumber++, GetValues(item), header: false);
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return output.ToArray();
    }

    private static string[] GetValues(CaseExportDto item)
    {
        return
        [
            item.ExternalCaseCode,
            item.ApplicantName,
            item.ProcedureFieldName ?? string.Empty,
            item.ProcedureName ?? string.Empty,
            item.DepartmentName ?? string.Empty,
            item.OrganizationName ?? string.Empty,
            item.AssigneeName ?? string.Empty,
            FormatDate(item.ReceivedAt),
            FormatDate(item.Deadline),
            FormatDate(item.AppointmentDate),
            FormatDate(item.CompletedAt),
            GetStatusLabel(item.Status)
        ];
    }

    private static string FormatDate(DateTime? value)
    {
        return value?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;
    }

    private static string GetStatusLabel(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.Received => "Mới tiếp nhận",
            CaseStatus.InProgress => "Đang xử lý",
            CaseStatus.Pending => "Chờ xử lý",
            CaseStatus.Completed => "Đã hoàn thành",
            CaseStatus.Overdue => "Quá hạn",
            CaseStatus.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };
    }

    private static string EscapeCsv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void WriteRow(
        XmlWriter writer,
        int rowNumber,
        IReadOnlyList<string> values,
        bool header)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", rowNumber.ToString());
        for (var index = 0; index < values.Count; index++)
        {
            writer.WriteStartElement("c");
            writer.WriteAttributeString("r", $"{GetColumnName(index + 1)}{rowNumber}");
            writer.WriteAttributeString("t", "inlineStr");
            if (header)
            {
                writer.WriteAttributeString("s", "1");
            }
            writer.WriteStartElement("is");
            writer.WriteStartElement("t");
            writer.WriteAttributeString("xml", "space", null, "preserve");
            writer.WriteString(values[index]);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private static string GetColumnName(int number)
    {
        var result = string.Empty;
        while (number > 0)
        {
            number--;
            result = (char)('A' + number % 26) + result;
            number /= 26;
        }
        return result;
    }

    private static void WriteTextEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private const string SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private const string ContentTypesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private const string RootRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string WorkbookXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets><sheet name="Hồ sơ" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """;

    private const string WorkbookRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private const string StylesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="2"><font/><font><b/></font></fonts>
          <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
          <borders count="1"><border/></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>
        </styleSheet>
        """;
}
