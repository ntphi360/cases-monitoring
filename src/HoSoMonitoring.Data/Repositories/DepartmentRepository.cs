using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;

namespace HoSoMonitoring.Data.Repositories;

public class DepartmentRepository
    : RepositoryBase<Department, int>, IDepartmentRepository
{
    public DepartmentRepository(HoSoMonitoringContext context)
        : base(context)
    {
    }
}
