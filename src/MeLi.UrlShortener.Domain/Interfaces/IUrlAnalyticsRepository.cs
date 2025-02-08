using MeLi.UrlShortener.Domain.Entities;

namespace MeLi.UrlShortener.Domain.Interfaces
{
    public interface IUrlAnalyticsRepository
    {
        /// <summary>
        /// Records a new access for a URL
        /// </summary>
        Task RecordAccessAsync(string shortCode, DateTime accessTime);

        /// <summary>
        /// Gets the analytics for a specific URL
        /// </summary>
        Task<UrlAnalytics?> GetAnalyticsAsync(string shortCode);

        /// <summary>
        /// Gets daily statistics for a URL within a date range
        /// </summary>
        Task<Dictionary<DateTime, DailyStatsInfo>> GetDailyStatsAsync(
            string shortCode,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Gets hourly statistics for a URL on a specific date
        /// </summary>
        Task<Dictionary<int, int>?> GetHourlyStatsAsync(
            string shortCode,
            DateTime date);
    }
}