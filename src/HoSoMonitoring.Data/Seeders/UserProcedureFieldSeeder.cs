using HoSoMonitoring.Core.Content;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Seeders;

public class UserProcedureFieldSeeder
{
    private static readonly (string UserFullName, string FieldName)[] Seeds =
    [
        ("Dương Thị Đào", "Hộ tịch"),
        ("Dương Thị Đào", "Chứng thực"),
        ("Nguyễn Quang Hoàn", "Đất đai"),
        ("Nguyễn Quang Hoàn", "Quy hoạch xây dựng, kiến trúc"),
        ("Trần Thị Yến Nhung", "Thành lập và hoạt động của hộ kinh doanh"),
        ("Trần Thị Yến Nhung", "Bảo trợ xã hội")
    ];

    public async Task SeedAsync(
        HoSoMonitoringContext context,
        IReadOnlyCollection<User> users,
        IReadOnlyCollection<ProcedureField> procedureFields,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var permissions = await context.UserProcedureFields
            .ToListAsync(cancellationToken);

        foreach (var seed in Seeds)
        {
            var user = users.First(item =>
                SeederText.Normalize(item.FullName)
                    == SeederText.Normalize(seed.UserFullName));
            var procedureField = procedureFields.First(item =>
                SeederText.NormalizeProcedureField(item.Name)
                    == SeederText.NormalizeProcedureField(seed.FieldName));
            if (permissions.Any(item =>
                    item.UserId == user.Id
                    && item.ProcedureFieldId == procedureField.Id))
            {
                continue;
            }

            var permission = new UserProcedureField
            {
                UserId = user.Id,
                ProcedureFieldId = procedureField.Id,
                CreatedAt = now
            };
            context.UserProcedureFields.Add(permission);
            permissions.Add(permission);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
