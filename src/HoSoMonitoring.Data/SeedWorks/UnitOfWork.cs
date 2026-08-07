using AutoMapper;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Core.SeedWorks;
using HoSoMonitoring.Data.Repositories;

namespace HoSoMonitoring.Data.SeedWorks
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HoSoMonitoringContext _context;

        public UnitOfWork(
            HoSoMonitoringContext context,
            IMapper mapper)
        {
            _context = context;
            Cases = new CaseRepository(context, mapper);
            Departments = new DepartmentRepository(context);
            ProcedureFields = new ProcedureFieldRepository(context);
            Procedures = new ProcedureRepository(context);
            Users = new UserRepository(context);
            CaseAssignments = new CaseAssignmentRepository(context);
            CaseHistories = new CaseHistoryRepository(context);
        }

        public ICaseRepository Cases { get; private set; }

        public IDepartmentRepository Departments { get; private set; }

        public IProcedureFieldRepository ProcedureFields { get; private set; }

        public IProcedureRepository Procedures { get; private set; }

        public IUserRepository Users { get; private set; }

        public ICaseAssignmentRepository CaseAssignments { get; private set; }

        public ICaseHistoryRepository CaseHistories { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        //public void Dispose()
        //{
        //    _context.Dispose();
        //}
    }
}
