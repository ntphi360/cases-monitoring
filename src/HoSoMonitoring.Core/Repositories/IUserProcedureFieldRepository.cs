using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.SeedWorks;

namespace HoSoMonitoring.Core.Repositories;

public interface IUserProcedureFieldRepository
    : IRepository<UserProcedureField, int>
{
    Task<List<UserProcedureField>> GetByUserIdAsync(int userId);

    Task<List<UserProcedureField>> GetByProcedureFieldIdAsync(int procedureFieldId);

    Task<bool> ExistsAsync(int userId, int procedureFieldId);

    Task<bool> CanUserHandleProcedureAsync(int userId, int procedureId);
}
