using HoSoMonitoring.Core.Repositories;

namespace HoSoMonitoring.Data.Repositories
{
    public interface IUnitOfWork
    {
        ICaseRepository Cases { get; }

        Task<int> CompleteAsync();
    }
}