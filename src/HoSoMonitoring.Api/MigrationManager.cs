using HoSoMonitoring.Data;
using HoSoMonitoring.Data.Seeders;
using Microsoft.EntityFrameworkCore;

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

        return app;
    }
}
