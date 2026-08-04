using HoSoMonitoring.Data;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Api
{
    public static class MigrationManager
    {
        public static WebApplication MigrateDatabase(
            this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                using (var context =
                    scope.ServiceProvider
                        .GetRequiredService<HoSoMonitoringContext>())
                {
                    context.Database.Migrate();

                    new DataSeeder()
                        .SeedAsync(context)
                        .Wait();
                }
            }

            return app;
        }
    }
}