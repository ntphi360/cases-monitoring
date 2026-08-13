using HoSoMonitoring.Core.Constants;
using HoSoMonitoring.Core.Content;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HoSoMonitoring.Data.Seeders;

public class IdentitySeeder
{
    public async Task SeedAsync(
        UserManager<User> userManager,
        RoleManager<AppRole> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        foreach (var roleName in new[] { Roles.Admin, Roles.Staff })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new AppRole { Name = roleName });
                EnsureSucceeded(result, $"Không thể tạo role {roleName}");
            }
        }

        var users = await userManager.Users.ToListAsync();
        foreach (var user in users)
        {
            user.UserName = user.Username;
            user.NormalizedUserName = userManager.NormalizeName(user.UserName);
            user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
            user.SecurityStamp ??= Guid.NewGuid().ToString();
            user.ConcurrencyStamp ??= Guid.NewGuid().ToString();
            EnsureSucceeded(await userManager.UpdateAsync(user), $"Không thể chuẩn hóa tài khoản {user.UserName}");

            var role = user.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase)
                ? Roles.Admin : Roles.Staff;
            if (!await userManager.IsInRoleAsync(user, role))
            {
                EnsureSucceeded(await userManager.AddToRoleAsync(user, role), $"Không thể gán role {role}");
            }
        }

        await SetSeedPasswordAsync(userManager, users, "admin", configuration["Auth:SeedAdminPassword"], logger);
        var staffPassword = configuration["Auth:SeedStaffPassword"];
        foreach (var username in new[] { "duongthidao", "nguyenquanghoan", "tranthiyennhung" })
        {
            await SetSeedPasswordAsync(userManager, users, username, staffPassword, logger);
        }
    }

    private static async Task SetSeedPasswordAsync(
        UserManager<User> manager, IEnumerable<User> users, string username,
        string? password, ILogger logger)
    {
        var user = users.FirstOrDefault(item => item.UserName!.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null || await manager.HasPasswordAsync(user)) return;
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Tài khoản {UserName} chưa có mật khẩu seed trong User Secrets.", username);
            return;
        }
        EnsureSucceeded(await manager.AddPasswordAsync(user, password), $"Không thể đặt mật khẩu cho {username}");
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{message}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
        }
    }
}
