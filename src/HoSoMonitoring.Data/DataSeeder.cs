using HoSoMonitoring.Core.Content;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace HoSoMonitoring.Data;

public class DataSeeder
{
    private const int DefaultProcessingHours = 8;

    private static readonly DepartmentSeed[] DepartmentSeeds =
    [
        new("PB001", "Văn phòng HĐND&UBND"),
        new("PB002", "Phòng Kinh tế, hạ tầng và đô thị"),
        new("PB003", "Phòng Văn hoá - Xã hội")
    ];

    private static readonly ProcedureFieldSeed[] ProcedureFieldSeeds =
    [
        new("LV001", "Hộ tịch"),
        new("LV002", "Chứng thực"),
        new("LV003", "Đất đai"),
        new("LV004", "Thành lập và hoạt động của hộ kinh doanh"),
        new("LV005", "Quy hoạch xây dựng, kiến trúc"),
        new("LV006", "Bảo trợ xã hội")
    ];

    private static readonly ProcedureSeed[] ProcedureSeeds =
    [
        new("Đăng ký khai tử", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Cấp bản sao Trích lục hộ tịch", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Thủ tục cấp Giấy xác nhận tình trạng hôn nhân", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Đăng ký kết hôn", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Đăng ký lại khai sinh", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Thủ tục thay đổi, cải chính, bổ sung thông tin hộ tịch", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Thủ tục đăng ký kết hôn có yếu tố nước ngoài", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Chứng thực bản sao từ bản chính giấy tờ, văn bản do cơ quan tổ chức có thẩm quyền của Việt Nam cấp hoặc chứng nhận", "Chứng thực", "Văn phòng HĐND&UBND"),
        new("Chứng thực chữ ký trong các giấy tờ, văn bản", "Chứng thực", "Văn phòng HĐND&UBND"),
        new("Đăng ký thành lập hộ kinh doanh", "Thành lập và hoạt động của hộ kinh doanh", "Phòng Kinh tế, hạ tầng và đô thị"),
        new("Đăng ký thay đổi nội dung đăng ký hộ kinh doanh", "Thành lập và hoạt động của hộ kinh doanh", "Phòng Kinh tế, hạ tầng và đô thị"),
        new("Chấm dứt hoạt động hộ kinh doanh", "Thành lập và hoạt động của hộ kinh doanh", "Phòng Kinh tế, hạ tầng và đô thị"),
        new("Đăng ký đất đai, tài sản gắn liền với đất, cấp Giấy chứng nhận quyền sử dụng đất, quyền sở hữu tài sản gắn liền với đất lần đầu đối với hộ gia đình, cá nhân, cộng đồng dân cư, người gốc Việt Nam định cư ở nước ngoài", "Đất đai", "Phòng Kinh tế, hạ tầng và đô thị"),
        new("Đính chính Giấy chứng nhận đã cấp lần đầu có sai sót", "Đất đai", "Phòng Kinh tế, hạ tầng và đô thị"),
        new("Hòa giải tranh chấp đất đai", "Đất đai", "Phòng Kinh tế, hạ tầng và đô thị"),
        new("Cung cấp thông tin về quy hoạch xây dựng thuộc thẩm quyền của UBND cấp xã", "Quy hoạch xây dựng, kiến trúc", "Phòng Kinh tế, hạ tầng và đô thị"),
        new("Hỗ trợ chi phí mai táng cho đối tượng bảo trợ xã hội", "Bảo trợ xã hội", "Phòng Văn hoá - Xã hội")
    ];

    public async Task SeedAsync(
        HoSoMonitoringContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var departments = await SeedDepartmentsAsync(context, now, cancellationToken);
        await SeedUserAsync(context, departments, now, cancellationToken);
        var procedureFields = await SeedProcedureFieldsAsync(
            context,
            cancellationToken);
        await SeedProceduresAsync(
            context,
            departments,
            procedureFields,
            now,
            cancellationToken);
    }

    private static async Task<List<Department>> SeedDepartmentsAsync(
        HoSoMonitoringContext context,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var departments = await context.Departments.ToListAsync(cancellationToken);

        var root = FindDepartment(departments, "ROOT", "Đơn vị quản trị");
        if (root == null)
        {
            root = new Department
            {
                Code = "ROOT",
                Name = "Đơn vị quản trị",
                ParentId = null,
                IsActive = true,
                CreatedAt = now
            };
            context.Departments.Add(root);
            departments.Add(root);
        }

        foreach (var seed in DepartmentSeeds)
        {
            var existing = FindDepartment(departments, seed.Code, seed.Name);
            if (existing != null)
            {
                if (!existing.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)
                    && NormalizeText(existing.Name) == NormalizeText(seed.Name)
                    && !departments.Any(department =>
                        department.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    existing.Code = seed.Code;
                }

                continue;
            }

            var department = new Department
            {
                Code = seed.Code,
                Name = seed.Name,
                ParentId = null,
                IsActive = true,
                CreatedAt = now
            };
            context.Departments.Add(department);
            departments.Add(department);
        }

        await context.SaveChangesAsync(cancellationToken);
        return departments;
    }

    private static async Task SeedUserAsync(
        HoSoMonitoringContext context,
        IReadOnlyCollection<Department> departments,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var adminExists = await context.Users.AnyAsync(
            user => user.Username == "admin",
            cancellationToken);
        if (adminExists)
        {
            return;
        }

        var root = FindDepartment(departments, "ROOT", "Đơn vị quản trị")
            ?? throw new InvalidOperationException("Không tìm thấy đơn vị ROOT để seed User.");

        context.Users.Add(new User
        {
            Username = "admin",
            FullName = "Quản trị hệ thống",
            Email = "admin@example.com",
            PhoneNumber = "0900000000",
            DepartmentId = root.Id,
            ExternalUserCode = "ADMIN001",
            IsActive = true,
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<ProcedureField>> SeedProcedureFieldsAsync(
        HoSoMonitoringContext context,
        CancellationToken cancellationToken)
    {
        var procedureFields = await context.ProcedureFields
            .ToListAsync(cancellationToken);

        foreach (var seed in ProcedureFieldSeeds)
        {
            var existing = FindProcedureField(procedureFields, seed.Code, seed.Name);
            if (existing != null)
            {
                // Chuẩn hóa mã của lĩnh vực trùng tên mà không tạo bản ghi duplicate.
                if (!existing.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)
                    && NormalizeProcedureFieldName(existing.Name)
                        == NormalizeProcedureFieldName(seed.Name)
                    && !procedureFields.Any(field =>
                        field.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    existing.Code = seed.Code;
                }

                continue;
            }

            var procedureField = new ProcedureField
            {
                Code = seed.Code,
                Name = seed.Name,
                IsActive = true
            };
            context.ProcedureFields.Add(procedureField);
            procedureFields.Add(procedureField);
        }

        await context.SaveChangesAsync(cancellationToken);
        return procedureFields;
    }

    private static async Task SeedProceduresAsync(
        HoSoMonitoringContext context,
        IReadOnlyCollection<Department> departments,
        IReadOnlyCollection<ProcedureField> procedureFields,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var procedures = await context.Procedures.ToListAsync(cancellationToken);
        var usedCodes = new HashSet<string>(
            procedures.Select(procedure => procedure.Code),
            StringComparer.OrdinalIgnoreCase);
        var nextCode = 1;

        foreach (var seed in ProcedureSeeds)
        {
            if (procedures.Any(procedure =>
                    NormalizeText(procedure.Name) == NormalizeText(seed.Name)))
            {
                continue;
            }

            var procedureField = procedureFields.FirstOrDefault(field =>
                NormalizeProcedureFieldName(field.Name)
                    == NormalizeProcedureFieldName(seed.ProcedureFieldName))
                ?? throw new InvalidOperationException(
                    $"Không tìm thấy lĩnh vực '{seed.ProcedureFieldName}'.");
            var department = departments.FirstOrDefault(item =>
                NormalizeText(item.Name) == NormalizeText(seed.DepartmentName))
                ?? throw new InvalidOperationException(
                    $"Không tìm thấy phòng ban '{seed.DepartmentName}'.");

            string code;
            do
            {
                code = nextCode.ToString("D4");
                nextCode++;
            }
            while (usedCodes.Contains(code));

            var procedure = new Procedure
            {
                Code = code,
                Name = seed.Name,
                ProcedureFieldId = procedureField.Id,
                DepartmentId = department.Id,
                DefaultProcessingHours = DefaultProcessingHours,
                IsActive = true,
                CreatedAt = now
            };
            context.Procedures.Add(procedure);
            procedures.Add(procedure);
            usedCodes.Add(code);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Department? FindDepartment(
        IEnumerable<Department> departments,
        string code,
        string name)
    {
        return departments.FirstOrDefault(department =>
            department.Code.Equals(code, StringComparison.OrdinalIgnoreCase)
            || NormalizeText(department.Name) == NormalizeText(name));
    }

    private static ProcedureField? FindProcedureField(
        IEnumerable<ProcedureField> procedureFields,
        string code,
        string name)
    {
        return procedureFields.FirstOrDefault(field =>
            field.Code.Equals(code, StringComparison.OrdinalIgnoreCase)
            || NormalizeProcedureFieldName(field.Name)
                == NormalizeProcedureFieldName(name));
    }

    private static string NormalizeProcedureFieldName(string? value)
    {
        var normalized = NormalizeText(value);
        return normalized == NormalizeText("Hộ tịch 2")
            ? NormalizeText("Hộ tịch")
            : normalized;
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

    private sealed record DepartmentSeed(string Code, string Name);

    private sealed record ProcedureFieldSeed(string Code, string Name);

    private sealed record ProcedureSeed(
        string Name,
        string ProcedureFieldName,
        string DepartmentName);
}
