using HoSoMonitoring.Core.Repositories;

namespace HoSoMonitoring.Core.SeedWorks
{
    public interface IUnitOfWork
    {
        ICaseRepository Cases { get; }

        Task<int> CompleteAsync();
    }
}