using HoSoMonitoring.Data;
using HoSoMonitoring.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using HoSoMonitoring.Core.Content;
using Microsoft.AspNetCore.Identity;

namespace HoSoMonitoring.Api;

public static class MigrationManager
{
    public static WebApplication MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        using var context = scope.ServiceProvider
            .GetRequiredService<HoSoMonitoringContext>();

        context.Database.Migrate();
        new DataSeeder()
            .SeedAsync(
                context,
                fixCaseDepartments: app.Environment.IsDevelopment())
            .Wait();
        new IdentitySeeder().SeedAsync(
            scope.ServiceProvider.GetRequiredService<UserManager<User>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>(),
            scope.ServiceProvider.GetRequiredService<IConfiguration>(),
            app.Environment,
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<IdentitySeeder>())
            .Wait();

        return app;
    }
}
