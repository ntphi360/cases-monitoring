namespace HoSoMonitoring.Core.Models
{
    public class PageResultBase
    {
        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }
    }
}