using MeLi.UrlShortener.Domain.Entities;

namespace MeLi.UrlShortener.Application.Cache
{
    public interface IUrlCache
    {
        Task<string> GetLongUrlAsync(string shortCode);
        Task SetUrlAsync(string shortCode, string longUrl, TimeSpan? ttl = null);
        Task DeleteLongUrlAsync(string shortCode);
        Task<T> GetAsync<T>(string cacheKey);
        Task SetAsync<T>(string cacheKey, T entity, TimeSpan? timeSpan = null);
    }
}
