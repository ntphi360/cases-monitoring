using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;

namespace HoSoMonitoring.Data.Repositories;

public class UserRepository
    : RepositoryBase<User, int>, IUserRepository
{
    public UserRepository(HoSoMonitoringContext context)
        : base(context)
    {
    }
}
