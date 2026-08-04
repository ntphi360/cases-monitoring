using HoSoMonitoring.Core.Content;

namespace HoSoMonitoring.Data
{
    public class DataSeeder
    {
        public async Task SeedAsync(HoSoMonitoringContext context)
        {
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

            if (!context.Procedures.Any())
            {
                var field = context.ProcedureFields
                    .First(x => x.Code == "HOTICH");

                await context.Procedures.AddAsync(new Procedure
                {
                    Code = "DKKS",
                    Name = "Đăng ký khai sinh",
                    ProcedureFieldId = field.Id,
                    DefaultProcessingHours = 8,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                await context.SaveChangesAsync();
            }
        }
    }
}