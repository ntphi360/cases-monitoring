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
        }

        public ICaseRepository Cases { get; private set; }

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