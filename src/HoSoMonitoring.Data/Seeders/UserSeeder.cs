using HoSoMonitoring.Core.Content;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Seeders;

public class UserSeeder
{
    private static readonly UserSeed[] Seeds =
    [
        new("duongthidao", "Dương Thị Đào", "duongthidao@example.com", "CB001", "PB001"),
        new("nguyenquanghoan", "Nguyễn Quang Hoàn", "nguyenquanghoan@example.com", "CB002", "PB002"),
        new("tranthiyennhung", "Trần Thị Yến Nhung", "tranthiyennhung@example.com", "CB003", "PB003")
    ];

    public async Task<List<User>> SeedAsync(
        HoSoMonitoringContext context,
        IReadOnlyCollection<Department> departments,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var users = await context.Users.ToListAsync(cancellationToken);
        var root = departments.First(item =>
            item.Code.Equals("ROOT", StringComparison.OrdinalIgnoreCase));
        if (!users.Any(item =>
                item.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)))
        {
            var admin = new User
            {
                Username = "admin",
                FullName = "Quản trị hệ thống",
                Email = "admin@example.com",
                PhoneNumber = "0900000000",
                DepartmentId = root.Id,
                ExternalUserCode = "ADMIN001",
                IsActive = true,
                CreatedAt = now
            };
            context.Users.Add(admin);
            users.Add(admin);
        }

        foreach (var seed in Seeds)
        {
            var department = departments.First(item =>
                item.Code.Equals(seed.DepartmentCode, StringComparison.OrdinalIgnoreCase));
            var existing = users.FirstOrDefault(item =>
                item.Username.Equals(seed.Username, StringComparison.OrdinalIgnoreCase)
                || SeederText.Normalize(item.FullName) == SeederText.Normalize(seed.FullName));
            if (existing != null)
            {
                existing.FullName = seed.FullName;
                existing.DepartmentId = department.Id;
                existing.ExternalUserCode = seed.ExternalUserCode;
                existing.IsActive = true;
                continue;
            }

            var user = new User
            {
                Username = seed.Username,
                FullName = seed.FullName,
                Email = seed.Email,
                DepartmentId = department.Id,
                ExternalUserCode = seed.ExternalUserCode,
                IsActive = true,
                CreatedAt = now
            };
            context.Users.Add(user);
            users.Add(user);
        }

        await context.SaveChangesAsync(cancellationToken);
        return users;
    }

    private sealed record UserSeed(
        string Username,
        string FullName,
        string Email,
        string ExternalUserCode,
        string DepartmentCode);
}
