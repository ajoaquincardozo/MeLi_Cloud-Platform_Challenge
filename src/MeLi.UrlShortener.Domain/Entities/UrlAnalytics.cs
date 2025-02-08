using System.ComponentModel.DataAnnotations;

namespace MeLi.UrlShortener.Domain.Entities
{
    public class UrlAnalytics
    {
        [Key]
        public string Id { get; protected set; } = default!;
        public string ShortCode { get; private set; }
        public List<DailyAccess> DailyAccesses { get; private set; }
        public DateTime LastCalculatedAt { get; private set; }
        public long TotalAccessCount { get; private set; } // Counter precalculado
    }

    public class DailyAccess
    {
        public DateTime Date { get; set; }
        public int[] HourlyHits { get; set; } = new int[24]; // Array fijo en lugar de Dictionary
        public int TotalDayHits { get; set; }
    }

    //Se esta usando como un DTO.
    public class DailyStatsInfo 
    {
        public Dictionary<int, int> HourlyAccesses { get; set; } = new();
        public int TotalDayAccesses => HourlyAccesses.Values.Sum();
    }
}
