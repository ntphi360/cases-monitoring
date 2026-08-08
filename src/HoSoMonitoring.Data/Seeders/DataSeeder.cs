namespace HoSoMonitoring.Data.Seeders;

public class DataSeeder
{
    public async Task<DataSeedResult> SeedAsync(
        HoSoMonitoringContext context,
        bool fixCaseDepartments = false,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var departments = await new DepartmentSeeder()
            .SeedAsync(context, now, cancellationToken);
        var procedureFields = await new ProcedureFieldSeeder()
            .SeedAsync(context, fixCaseDepartments, cancellationToken);
        var caseFixupCount = await new ProcedureSeeder()
            .SeedAsync(
                context,
                departments,
                procedureFields,
                now,
                fixCaseDepartments,
                cancellationToken);
        var users = await new UserSeeder()
            .SeedAsync(context, departments, now, cancellationToken);
        await new UserProcedureFieldSeeder()
            .SeedAsync(context, users, procedureFields, now, cancellationToken);

        return new DataSeedResult(caseFixupCount);
    }
}

public record DataSeedResult(int CaseDepartmentFixupCount);
