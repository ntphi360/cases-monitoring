using HoSoMonitoring.Core.Content;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Seeders;

public class DepartmentSeeder
{
    private static readonly (string Code, string Name)[] Seeds =
    [
        ("PB001", "Văn phòng HĐND&UBND"),
        ("PB002", "Phòng Kinh tế, hạ tầng và đô thị"),
        ("PB003", "Phòng Văn hoá - Xã hội")
    ];

    public async Task<List<Department>> SeedAsync(
        HoSoMonitoringContext context,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var departments = await context.Departments.ToListAsync(cancellationToken);
        if (!departments.Any(item =>
                item.Code.Equals("ROOT", StringComparison.OrdinalIgnoreCase)))
        {
            var root = new Department
            {
                Code = "ROOT",
                Name = "Đơn vị quản trị",
                IsActive = true,
                CreatedAt = now
            };
            context.Departments.Add(root);
            departments.Add(root);
        }

        foreach (var seed in Seeds)
        {
            var existing = departments.FirstOrDefault(item =>
                item.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)
                || SeederText.Normalize(item.Name) == SeederText.Normalize(seed.Name));
            if (existing != null)
            {
                if (!existing.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)
                    && SeederText.Normalize(existing.Name) == SeederText.Normalize(seed.Name)
                    && !departments.Any(item =>
                        item.Code.Equals(seed.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    existing.Code = seed.Code;
                }

                continue;
            }

            var department = new Department
            {
                Code = seed.Code,
                Name = seed.Name,
                IsActive = true,
                CreatedAt = now
            };
            context.Departments.Add(department);
            departments.Add(department);
        }

        await context.SaveChangesAsync(cancellationToken);
        return departments;
    }
}
