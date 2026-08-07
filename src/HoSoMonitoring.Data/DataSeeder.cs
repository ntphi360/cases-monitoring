using HoSoMonitoring.Core.Content;

namespace HoSoMonitoring.Data
{
    public class DataSeeder
    {
        public async Task SeedAsync(HoSoMonitoringContext context)
        {
            // 1. Seed Department
            if (!context.Departments.Any())
            {
                await context.Departments.AddAsync(new Department
                {
                    Code = "ROOT",
                    Name = "Đơn vị quản trị",
                    ParentId = null,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                await context.SaveChangesAsync();
            }

            // 2. Seed User
            if (!context.Users.Any())
            {
                var department = context.Departments
                    .First(x => x.Code == "ROOT");

                await context.Users.AddAsync(new User
                {
                    Username = "admin",
                    FullName = "Quản trị hệ thống",
                    Email = "admin@example.com",
                    PhoneNumber = "0900000000",
                    DepartmentId = department.Id,
                    ExternalUserCode = "ADMIN001",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                await context.SaveChangesAsync();
            }

            // 3. Seed ProcedureField
            if (!context.ProcedureFields.Any())
            {
                await context.ProcedureFields.AddAsync(new ProcedureField
                {
                    Code = "HOTICH",
                    Name = "Hộ tịch",
                    IsActive = true
                });

                await context.SaveChangesAsync();
            }

            // 4. Seed Procedure
            if (!context.Procedures.Any())
            {
                var field = context.ProcedureFields
                    .First(x => x.Code == "HOTICH");

                var department = context.Departments
                    .First(x => x.Code == "ROOT");

                await context.Procedures.AddAsync(new Procedure
                {
                    Code = "DKKS",
                    Name = "Đăng ký khai sinh",
                    ProcedureFieldId = field.Id,
                    DepartmentId = department.Id,
                    DefaultProcessingHours = 8,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                await context.SaveChangesAsync();
            }
        }
    }
}