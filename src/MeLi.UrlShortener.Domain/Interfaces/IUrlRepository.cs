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
        Task<UrlEntity> GetByShortCodeAsync(string shortCode);

        /// <summary>
        /// Checks if a short code already exists
        /// </summary>
        Task<bool> ExistsAsync(string shortCode);

        /// <summary>
        /// Soft deletes a URL by its short code
        /// </summary>
        Task<bool> DeleteAsync(string shortCode);
    }

    public class UrlStatistics
    {
        public long TotalAccesses { get; set; }
        public DateTime? LastAccessed { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}