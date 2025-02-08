using MeLi.UrlShortener.Application.Cache;
using MeLi.UrlShortener.Application.Interfaces;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MeLi.UrlShortener.Application.Services
{
    public class UrlAnalyticsService : IUrlAnalyticsService
    {
        private readonly IUrlAnalyticsRepository _analyticsRepository;
        private readonly ILogger<UrlAnalyticsService> _logger;
        private readonly IUrlCache _cache;

        public UrlAnalyticsService(
            IUrlAnalyticsRepository analyticsRepository,
            IUrlCache cache,
            ILogger<UrlAnalyticsService> logger)
        {
            _analyticsRepository = analyticsRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task RecordAccessAsync(string shortCode)
        {
            try
            {
                // Fire and forget - no await
                _ = _analyticsRepository.RecordAccessAsync(shortCode, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording access for shortCode: {ShortCode}", shortCode);
                // No rethrow - analytics errors shouldn't affect main flow
            }
        }

        public async Task<UrlAnalytics?> GetAnalyticsAsync(string shortCode)
        {
            try
            {
                // Try get from cache first
                var cacheKey = $"analytics:{shortCode}";
                var cachedAnalytics = await _cache.GetAsync<UrlAnalytics>(cacheKey);
                if (cachedAnalytics != null)
                    return cachedAnalytics;

                // Get from repository
                var analytics = await _analyticsRepository.GetAnalyticsAsync(shortCode);
                if (analytics != null)
                {
                    // Cache for future requests (short TTL as data changes frequently)
                    await _cache.SetAsync(cacheKey, analytics, TimeSpan.FromMinutes(1));
                }

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics for shortCode: {ShortCode}", shortCode);
                throw;
            }
        }

        public async Task<Dictionary<DateTime, DailyStatsInfo>> GetDailyStatsAsync(
            string shortCode,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.UtcNow.AddDays(-30);
                endDate ??= DateTime.UtcNow;

                return await _analyticsRepository.GetDailyStatsAsync(
                    shortCode,
                    startDate.Value,
                    endDate.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting daily stats for shortCode: {ShortCode}, startDate: {StartDate}, endDate: {EndDate}",
                    shortCode, startDate, endDate);
                throw;
            }
        }

        public async Task<Dictionary<int, int>?> GetHourlyStatsAsync(
            string shortCode,
            DateTime date)
        {
            try
            {
                var cacheKey = $"hourly:{shortCode}:{date:yyyy-MM-dd}";
                var cachedStats = await _cache.GetAsync<Dictionary<int, int>>(cacheKey);
                if (cachedStats != null)
                    return cachedStats;

                var stats = await _analyticsRepository.GetHourlyStatsAsync(shortCode, date);
                if (stats != null)
                {
                    // Cache hourly stats for longer as historical data changes less frequently
                    await _cache.SetAsync(cacheKey, stats, TimeSpan.FromHours(1));
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting hourly stats for shortCode: {ShortCode}, date: {Date}",
                    shortCode, date);
                throw;
            }
        }

        public async Task<UrlStatsSummary> GetStatsSummaryAsync(string shortCode)
        {
            try
            {
                var analytics = await GetAnalyticsAsync(shortCode);
                if (analytics == null)
                    return new UrlStatsSummary();

                return new UrlStatsSummary
                {
                    TotalAccesses = analytics.TotalAccessCount,
                    LastAccessed = analytics.LastCalculatedAt,
                    // Additional stats can be added here as needed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats summary for shortCode: {ShortCode}", shortCode);
                throw;
            }
        }
    }
}