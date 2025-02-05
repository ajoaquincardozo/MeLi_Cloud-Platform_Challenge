using MeLi.UrlShortener.Application.DTOs;

namespace MeLi.UrlShortener.Application.Interfaces
{
    public interface IUrlService
    {
        Task<UrlResponseDto> CreateShortUrlAsync(CreateShortUrlRequest request);
        Task<string> GetLongUrlAsync(string shortCode);
        Task<bool> DeleteUrlAsync(string shortCode);
        Task<UrlResponseDto> GetUrlStatsAsync(string shortCode);
    }
}