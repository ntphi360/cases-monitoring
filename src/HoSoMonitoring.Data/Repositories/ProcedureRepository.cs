using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;

namespace HoSoMonitoring.Data.Repositories;

public class ProcedureRepository
    : RepositoryBase<Procedure, int>, IProcedureRepository
{
    public ProcedureRepository(HoSoMonitoringContext context)
        : base(context)
    {
    }
}
