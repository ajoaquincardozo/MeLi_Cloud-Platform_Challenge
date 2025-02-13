using MeLi.UrlShortener.Application.DTOs;

namespace MeLi.UrlShortener.Application.Interfaces
{
    public interface IUrlService
    {
        Task<string> CreateShortUrlAsync(CreateShortUrlRequest request);
        Task<string> GetLongUrlAsync(string shortCode);
        Task<bool> DeleteUrlAsync(string shortCode);
    }
}