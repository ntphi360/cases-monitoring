using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;

namespace HoSoMonitoring.Data.Repositories;

public class ProcedureFieldRepository
    : RepositoryBase<ProcedureField, int>, IProcedureFieldRepository
{
    public ProcedureFieldRepository(HoSoMonitoringContext context)
        : base(context)
    {
    }
}
