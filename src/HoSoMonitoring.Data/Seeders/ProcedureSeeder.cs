using HoSoMonitoring.Core.Content;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Seeders;

public class ProcedureSeeder
{
    private const int DefaultProcessingHours = 8;

    private static readonly ProcedureSeed[] Seeds =
    [
        new("Đăng ký khai sinh", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Đăng ký khai tử", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Cấp bản sao Trích lục hộ tịch", "Hộ tịch", "Văn phòng HĐND&UBND"),
        new("Cấp bản sao Trích lục hộ tịch, bản sao Giấy khai sinh", "Hộ tịch", "Văn phòng HĐND&UBND"),
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

    public async Task<int> SeedAsync(
        HoSoMonitoringContext context,
        IReadOnlyCollection<Department> departments,
        IReadOnlyCollection<ProcedureField> procedureFields,
        DateTime now,
        bool fixCaseDepartments,
        CancellationToken cancellationToken = default)
    {
        var procedures = await context.Procedures.ToListAsync(cancellationToken);
        var usedCodes = new HashSet<string>(
            procedures.Select(item => item.Code),
            StringComparer.OrdinalIgnoreCase);
        var nextCode = 1;

        foreach (var seed in Seeds)
        {
            var procedureField = procedureFields.First(item =>
                SeederText.NormalizeProcedureField(item.Name)
                    == SeederText.NormalizeProcedureField(seed.ProcedureFieldName));
            var department = departments.First(item =>
                SeederText.Normalize(item.Name)
                    == SeederText.Normalize(seed.DepartmentName));
            var existing = procedures.FirstOrDefault(item =>
                SeederText.Normalize(item.Name) == SeederText.Normalize(seed.Name));

            if (existing != null)
            {
                // Sửa mapping nghiệp vụ, giữ nguyên Code và thời gian xử lý đã có.
                existing.ProcedureFieldId = procedureField.Id;
                existing.DepartmentId = department.Id;
                continue;
            }

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

        if (!fixCaseDepartments)
        {
            return 0;
        }

        var casesToFix = await context.Cases
            .Include(item => item.Procedure)
            .Where(item => item.Procedure != null
                && item.DepartmentId != item.Procedure.DepartmentId)
            .ToListAsync(cancellationToken);
        foreach (var caseEntity in casesToFix)
        {
            caseEntity.DepartmentId = caseEntity.Procedure!.DepartmentId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return casesToFix.Count;
    }

    private sealed record ProcedureSeed(
        string Name,
        string ProcedureFieldName,
        string DepartmentName);
}
