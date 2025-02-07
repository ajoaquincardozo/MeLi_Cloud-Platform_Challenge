namespace MeLi.UrlShortener.Application.Cache
{
    public interface IUrlCache
    {
        Task<string> GetLongUrlAsync(string shortCode);
        Task SetUrlAsync(string shortCode, string longUrl, TimeSpan? ttl = null);
        Task DeleteLongUrlAsync(string shortCode);
    }
}
