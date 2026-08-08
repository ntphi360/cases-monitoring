using HoSoMonitoring.Core.Content;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Seeders;

public class ProcedureFieldSeeder
{
    private static readonly (string Code, string Name)[] Seeds =
    [
        ("LV001", "Hộ tịch"),
        ("LV002", "Chứng thực"),
        ("LV003", "Đất đai"),
        ("LV004", "Thành lập và hoạt động của hộ kinh doanh"),
        ("LV005", "Quy hoạch xây dựng, kiến trúc"),
        ("LV006", "Bảo trợ xã hội")
    ];

    public async Task<List<ProcedureField>> SeedAsync(
        HoSoMonitoringContext context,
        bool consolidateDuplicates,
        CancellationToken cancellationToken = default)
    {
        var procedureFields = await context.ProcedureFields
            .ToListAsync(cancellationToken);

        foreach (var seed in Seeds)
        {
            var existing = procedureFields.FirstOrDefault(item =>
                item.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)
                || SeederText.NormalizeProcedureField(item.Name)
                    == SeederText.NormalizeProcedureField(seed.Name));
            if (existing != null)
            {
                if (!existing.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)
                    && SeederText.NormalizeProcedureField(existing.Name)
                        == SeederText.NormalizeProcedureField(seed.Name)
                    && !procedureFields.Any(item =>
                        item.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    existing.Code = seed.Code;
                }

                if (SeederText.Normalize(existing.Name)
                    == SeederText.Normalize("Hộ tịch 2"))
                {
                    existing.Name = "Hộ tịch";
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

        if (consolidateDuplicates)
        {
            await RedirectDuplicateCivilStatusFieldsAsync(
                context,
                procedureFields,
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return procedureFields;
    }

    private static async Task RedirectDuplicateCivilStatusFieldsAsync(
        HoSoMonitoringContext context,
        List<ProcedureField> procedureFields,
        CancellationToken cancellationToken)
    {
        var canonicalField = procedureFields.First(item =>
            item.Code.Equals("LV001", StringComparison.OrdinalIgnoreCase));
        var duplicateFields = procedureFields
            .Where(item => item.Id != canonicalField.Id
                && SeederText.NormalizeProcedureField(item.Name)
                    == SeederText.NormalizeProcedureField(canonicalField.Name))
            .ToList();

        foreach (var duplicateField in duplicateFields)
        {
            var procedures = await context.Procedures
                .Where(item => item.ProcedureFieldId == duplicateField.Id)
                .ToListAsync(cancellationToken);
            foreach (var procedure in procedures)
            {
                procedure.ProcedureFieldId = canonicalField.Id;
            }

            var duplicatePermissions = await context.UserProcedureFields
                .Where(item => item.ProcedureFieldId == duplicateField.Id)
                .ToListAsync(cancellationToken);
            foreach (var permission in duplicatePermissions)
            {
                var canonicalPermissionExists = await context.UserProcedureFields
                    .AnyAsync(item =>
                        item.UserId == permission.UserId
                        && item.ProcedureFieldId == canonicalField.Id,
                        cancellationToken);
                if (canonicalPermissionExists)
                {
                    continue;
                }

                permission.ProcedureFieldId = canonicalField.Id;
            }

            // Giữ bản ghi cũ để không xóa dữ liệu, nhưng không dùng cho mapping mới.
            duplicateField.IsActive = false;
        }
    }
}
