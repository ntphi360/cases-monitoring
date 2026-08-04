using HoSoMonitoring.Core.SeedWorks;

namespace HoSoMonitoring.Data.SeedWorks
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HoSoMonitoringContext _context;

        public UnitOfWork(HoSoMonitoringContext context)
        {
            _context = context;
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}