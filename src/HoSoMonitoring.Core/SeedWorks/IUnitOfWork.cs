namespace HoSoMonitoring.Core.SeedWorks
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> CompleteAsync();
    }
}