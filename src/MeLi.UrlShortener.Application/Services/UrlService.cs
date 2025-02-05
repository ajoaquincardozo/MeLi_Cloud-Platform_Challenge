using MeLi.UrlShortener.Application.DTOs;
using MeLi.UrlShortener.Application.Interfaces;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Domain.Interfaces;

namespace MeLi.UrlShortener.Application.Services
{
    public class UrlService : IUrlService
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IShortCodeGenerator _codeGenerator;
        private readonly IUrlValidator _urlValidator;

        public UrlService(
            IUrlRepository urlRepository,
            IShortCodeGenerator codeGenerator,
            IUrlValidator urlValidator)
        {
            _urlRepository = urlRepository;
            _codeGenerator = codeGenerator;
            _urlValidator = urlValidator;
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
                $"https://me.li/{shortCode}",
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
                $"https://me.li/{urlEntity.ShortCode}",
                urlEntity.LongUrl.Value,
                urlEntity.CreatedAt,
                urlEntity.AccessCount
            );
        }
    }
}