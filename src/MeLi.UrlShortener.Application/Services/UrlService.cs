using MeLi.UrlShortener.Application.Config;
using MeLi.UrlShortener.Application.DTOs;
using MeLi.UrlShortener.Application.Interfaces;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace MeLi.UrlShortener.Application.Services
{
    public class UrlService : IUrlService
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IShortCodeGenerator _codeGenerator;
        private readonly IUrlValidator _urlValidator;
        private readonly GeneralConfig _generalConfig;

        public UrlService(
            IUrlRepository urlRepository,
            IShortCodeGenerator codeGenerator,
            IUrlValidator urlValidator,
            IOptions<GeneralConfig> generalConfig)
        {
            _urlRepository = urlRepository ?? throw new ArgumentNullException(nameof(urlRepository));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _urlValidator = urlValidator ?? throw new ArgumentNullException(nameof(urlValidator));
            _generalConfig = generalConfig?.Value ?? throw new ArgumentNullException(nameof(generalConfig));
        }

        public async Task<UrlResponseDto> CreateShortUrlAsync(CreateShortUrlRequest request)
        {
            if (!_urlValidator.IsValidUrl(request.LongUrl))
                throw new ArgumentException("Invalid URL format");

            string shortCode;
            do
            {
                shortCode = _codeGenerator.GenerateCode();
            } while (await _urlRepository.ExistsAsync(shortCode));

            var urlEntity = UrlEntity.Create(request.LongUrl, shortCode);
            await _urlRepository.SaveAsync(urlEntity);

            return new UrlResponseDto(
                $"{_generalConfig.BaseUrl}/api/url/{shortCode}",
                urlEntity.LongUrl.Value,
                urlEntity.CreatedAt,
                urlEntity.AccessCount
            );
        }

        public async Task<string> GetLongUrlAsync(string shortCode)
        {
            var urlEntity = await _urlRepository.GetByShortCodeAsync(shortCode)
                ?? throw new KeyNotFoundException("Short URL not found");

            await _urlRepository.IncrementAccessCountAsync(shortCode);
            return urlEntity.LongUrl.Value;
        }

        public async Task<bool> DeleteUrlAsync(string shortCode)
        {
            return await _urlRepository.DeleteAsync(shortCode);
        }

        public async Task<UrlResponseDto> GetUrlStatsAsync(string shortCode)
        {
            var urlEntity = await _urlRepository.GetByShortCodeAsync(shortCode)
                ?? throw new KeyNotFoundException("Short URL not found");

            return new UrlResponseDto(
                $"{_generalConfig.BaseUrl}/api/url/{urlEntity.ShortCode}",
                urlEntity.LongUrl.Value,
                urlEntity.CreatedAt,
                urlEntity.AccessCount
            );
        }
    }
}