using PracticeLogger.Models.Api;

namespace PracticeLogger.Models
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasPrev { get; set; }
        public bool HasNext { get; set; }
        public string? Sort { get; set; }
        public bool Desc { get; set; }
        public string? Query { get; set; }
        public int? InstrumentId { get; set; }
        public IEnumerable<LinkDto>? Links { get; set; }
    }
}
