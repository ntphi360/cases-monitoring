using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.SeedWorks;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Repositories;

public class UserProcedureFieldRepository
    : RepositoryBase<UserProcedureField, int>, IUserProcedureFieldRepository
{
    public UserProcedureFieldRepository(HoSoMonitoringContext context)
        : base(context)
    {
    }

    public Task<List<UserProcedureField>> GetByUserIdAsync(int userId)
    {
        return _context.UserProcedureFields
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.ProcedureField)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ProcedureField!.Name)
            .ToListAsync();
    }

    public Task<List<UserProcedureField>> GetByProcedureFieldIdAsync(
        int procedureFieldId)
    {
        return _context.UserProcedureFields
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.ProcedureField)
            .Where(x => x.ProcedureFieldId == procedureFieldId)
            .OrderBy(x => x.User!.FullName)
            .ToListAsync();
    }

    public Task<bool> ExistsAsync(int userId, int procedureFieldId)
    {
        return _context.UserProcedureFields.AnyAsync(x =>
            x.UserId == userId
            && x.ProcedureFieldId == procedureFieldId);
    }

    public Task<bool> CanUserHandleProcedureAsync(int userId, int procedureId)
    {
        return _context.Procedures.AnyAsync(procedure =>
            procedure.Id == procedureId
            && _context.UserProcedureFields.Any(permission =>
                permission.UserId == userId
                && permission.ProcedureFieldId == procedure.ProcedureFieldId));
    }
}
