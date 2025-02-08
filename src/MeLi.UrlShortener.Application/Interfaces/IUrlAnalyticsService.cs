using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeLi.UrlShortener.Domain.Entities;

namespace MeLi.UrlShortener.Application.Interfaces
{
    public interface IUrlAnalyticsService
    {
        /// <summary>
        /// Records a new access to a URL
        /// </summary>
        Task RecordAccessAsync(string shortCode);

        /// <summary>
        /// Gets complete analytics for a URL
        /// </summary>
        Task<UrlAnalytics> GetAnalyticsAsync(string shortCode);

        /// <summary>
        /// Gets daily statistics for a URL within a date range
        /// </summary>
        Task<Dictionary<DateTime, DailyStatsInfo>> GetDailyStatsAsync(
            string shortCode,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Gets hourly statistics for a URL on a specific date
        /// </summary>
        Task<Dictionary<int, int>?> GetHourlyStatsAsync(
            string shortCode,
            DateTime date);

        /// <summary>
        /// Gets a summary of URL statistics
        /// </summary>
        Task<UrlStatsSummary> GetStatsSummaryAsync(string shortCode);
    }

    public class UrlStatsSummary
    {
        public long TotalAccesses { get; set; }
        public DateTime? LastAccessed { get; set; }
    }
}
