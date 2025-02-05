using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Domain.Interfaces;
using System.Xml;

namespace MeLi.UrlShortener.Domain.Interfaces
{
    public interface IUrlRepository
    {
        /// <summary>
        /// Saves a new URL entity to the repository
        /// </summary>
        Task<UrlEntity> SaveAsync(UrlEntity url);

        /// <summary>
        /// Retrieves a URL entity by its short code
        /// </summary>
        Task<UrlEntity?> GetByShortCodeAsync(string shortCode);

        /// <summary>
        /// Checks if a short code already exists
        /// </summary>
        Task<bool> ExistsAsync(string shortCode);

        /// <summary>
        /// Soft deletes a URL by its short code
        /// </summary>
        Task<bool> DeleteAsync(string shortCode);

        /// <summary>
        /// Increments the access count for a URL
        /// </summary>
        Task IncrementAccessCountAsync(string shortCode);

        /// <summary>
        /// Get access statistics for a specific short code
        /// </summary>
        Task<UrlStatistics?> GetStatisticsAsync(string shortCode);

        /// <summary>
        /// Get list of most accessed URLs with pagination
        /// </summary>
        Task<(List<UrlEntity> Urls, int TotalCount)> GetMostAccessedAsync(int page, int pageSize);
    }

    public class UrlStatistics
    {
        public long TotalAccesses { get; set; }
        public DateTime? LastAccessed { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}