using HoSoMonitoring.Core.Constants;
using HoSoMonitoring.Core.Content;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HoSoMonitoring.Data.Seeders;

public class IdentitySeeder
{
    private static readonly string[] StaffUsernames =
        ["duongthidao", "nguyenquanghoan", "tranthiyennhung"];

    public async Task SeedAsync(
        UserManager<User> userManager,
        RoleManager<AppRole> roleManager,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        foreach (var roleName in new[] { Roles.Admin, Roles.Staff })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new AppRole { Name = roleName });
                EnsureSucceeded(result, $"Không thể tạo role {roleName}", logger);
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
            EnsureSucceeded(
                await userManager.UpdateAsync(user),
                $"Không thể chuẩn hóa tài khoản {user.UserName}",
                logger);

            var role = user.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase)
                ? Roles.Admin : Roles.Staff;
            if (!await userManager.IsInRoleAsync(user, role))
            {
                EnsureSucceeded(
                    await userManager.AddToRoleAsync(user, role),
                    $"Không thể gán role {role}",
                    logger);
            }
        }

        var resetSeedPasswords = environment.IsDevelopment()
            && configuration.GetValue<bool>("Auth:ResetSeedPasswords");
        var adminPassword = configuration["Auth:SeedAdminPassword"];
        var staffPassword = configuration["Auth:SeedStaffPassword"];

        if (resetSeedPasswords)
        {
            EnsureResetPasswordConfigured(adminPassword, "Auth:SeedAdminPassword", logger);
            EnsureResetPasswordConfigured(staffPassword, "Auth:SeedStaffPassword", logger);
        }

        var seedUsernames = new[] { "admin" }.Concat(StaffUsernames);
        foreach (var username in seedUsernames)
        {
            var user = FindSeedUser(users, username);
            if (user == null)
            {
                logger.LogWarning("Không tìm thấy tài khoản seed {UserName}.", username);
                continue;
            }

            if (environment.IsDevelopment())
            {
                await ConfigureDevelopmentLockoutAsync(
                    userManager,
                    user,
                    logger);
            }

            await SetSeedPasswordAsync(
                userManager,
                user,
                username == "admin" ? adminPassword : staffPassword,
                resetSeedPasswords,
                logger);

            if (resetSeedPasswords)
            {
                await UnlockSeedAccountAsync(userManager, user, logger);
            }
        }
    }

    private static async Task SetSeedPasswordAsync(
        UserManager<User> manager,
        User user,
        string? password,
        bool resetExistingPassword,
        ILogger logger)
    {
        var hasPassword = await manager.HasPasswordAsync(user);
        if (hasPassword && !resetExistingPassword) return;

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Tài khoản {UserName} chưa có mật khẩu seed trong cấu hình.",
                user.UserName);
            return;
        }

        IdentityResult result;
        if (hasPassword)
        {
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            result = await manager.ResetPasswordAsync(user, token, password);
        }
        else
        {
            result = await manager.AddPasswordAsync(user, password);
        }

        EnsureSucceeded(
            result,
            resetExistingPassword
                ? $"Không thể reset mật khẩu seed cho {user.UserName}"
                : $"Không thể đặt mật khẩu seed cho {user.UserName}",
            logger);

        logger.LogInformation(
            resetExistingPassword
                ? "Đã reset mật khẩu seed cho {UserName}."
                : "Đã thêm mật khẩu seed cho {UserName}.",
            user.UserName);
    }

    private static async Task ConfigureDevelopmentLockoutAsync(
        UserManager<User> manager,
        User user,
        ILogger logger)
    {
        if (!user.LockoutEnabled)
        {
            EnsureSucceeded(
                await manager.SetLockoutEnabledAsync(user, true),
                $"Không thể bật lockout cho tài khoản seed {user.UserName}",
                logger);
        }
    }

    private static async Task UnlockSeedAccountAsync(
        UserManager<User> manager,
        User user,
        ILogger logger)
    {
        EnsureSucceeded(
            await manager.SetLockoutEndDateAsync(user, null),
            $"Không thể xóa thời gian khóa của tài khoản seed {user.UserName}",
            logger);
        EnsureSucceeded(
            await manager.ResetAccessFailedCountAsync(user),
            $"Không thể reset số lần đăng nhập thất bại của tài khoản seed {user.UserName}",
            logger);
    }

    private static User? FindSeedUser(IEnumerable<User> users, string username) =>
        users.FirstOrDefault(item =>
            string.Equals(item.UserName, username, StringComparison.OrdinalIgnoreCase));

    private static void EnsureResetPasswordConfigured(
        string? password,
        string configurationKey,
        ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(password)) return;

        logger.LogError(
            "Không thể reset mật khẩu seed vì thiếu cấu hình {ConfigurationKey}.",
            configurationKey);
        throw new InvalidOperationException(
            $"Thiếu cấu hình {configurationKey} để reset mật khẩu seed.");
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string message,
        ILogger logger)
    {
        if (result.Succeeded) return;

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        logger.LogError("{Message}: {Errors}", message, errors);
        throw new InvalidOperationException($"{message}: {errors}");
    }
}
