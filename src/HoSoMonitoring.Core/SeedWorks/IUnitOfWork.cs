using HoSoMonitoring.Core.Repositories;

namespace HoSoMonitoring.Core.SeedWorks
{
    public interface IUnitOfWork
    {
        ICaseRepository Cases { get; }

        IDepartmentRepository Departments { get; }

        IProcedureFieldRepository ProcedureFields { get; }

        IProcedureRepository Procedures { get; }

        IUserRepository Users { get; }

        ICaseAssignmentRepository CaseAssignments { get; }

        ICaseHistoryRepository CaseHistories { get; }

        INotificationRepository Notifications { get; }

        IUserProcedureFieldRepository UserProcedureFields { get; }

        Task<int> CompleteAsync();
    }
}
