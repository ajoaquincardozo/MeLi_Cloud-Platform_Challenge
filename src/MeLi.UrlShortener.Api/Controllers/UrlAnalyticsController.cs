using MeLi.UrlShortener.Application.Interfaces;
using MeLi.UrlShortener.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MeLi.UrlShortener.Api.Controllers
{
    [ApiController]
    [Route("analytics/urls")]
    public class UrlAnalyticsController : ControllerBase
    {
        private readonly IUrlAnalyticsService _analyticsService;
        private readonly ILogger<UrlAnalyticsController> _logger;

        public UrlAnalyticsController(
            IUrlAnalyticsService analyticsService,
            ILogger<UrlAnalyticsController> logger)
        {
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a summary of URL statistics
        /// </summary>
        [HttpGet("{shortCode}/summary")]
        [ProducesResponseType(typeof(UrlStatsSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatsSummary(
            [Required] string shortCode)
        {
            try
            {
                var summary = await _analyticsService.GetStatsSummaryAsync(shortCode);
                if (summary == null)
                    return NotFound();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats summary for shortCode: {ShortCode}", shortCode);
                throw;
            }
        }

        /// <summary>
        /// Gets daily statistics for a URL within a date range
        /// </summary>
        [HttpGet("{shortCode}/daily")]
        [ProducesResponseType(typeof(Dictionary<DateTime, DailyStatsInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDailyStats(
            [Required] string shortCode,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return BadRequest("Start date must be before end date");
                }

                var stats = await _analyticsService.GetDailyStatsAsync(shortCode, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting daily stats for shortCode: {ShortCode}, startDate: {StartDate}, endDate: {EndDate}",
                    shortCode, startDate, endDate);
                throw;
            }
        }

        /// <summary>
        /// Gets hourly statistics for a URL on a specific date
        /// </summary>
        [HttpGet("{shortCode}/hourly")]
        [ProducesResponseType(typeof(Dictionary<int, int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHourlyStats(
            [Required] string shortCode,
            [Required][FromQuery] DateTime date)
        {
            try
            {
                var stats = await _analyticsService.GetHourlyStatsAsync(shortCode, date);
                if (stats == null)
                    return NotFound();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting hourly stats for shortCode: {ShortCode}, date: {Date}",
                    shortCode, date);
                throw;
            }
        }

        /// <summary>
        /// Gets complete analytics for a URL
        /// </summary>
        [HttpGet("{shortCode}")]
        [ProducesResponseType(typeof(UrlAnalytics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAnalytics(
            [Required] string shortCode)
        {
            try
            {
                var analytics = await _analyticsService.GetAnalyticsAsync(shortCode);
                if (analytics == null)
                    return NotFound();

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics for shortCode: {ShortCode}", shortCode);
                throw;
            }
        }
    }
}