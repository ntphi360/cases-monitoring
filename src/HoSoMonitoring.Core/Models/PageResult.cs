namespace HoSoMonitoring.Core.Models
{
    public class PageResult<T> : PageResultBase
    {
        public List<T> Results { get; set; } = new();
    }
}