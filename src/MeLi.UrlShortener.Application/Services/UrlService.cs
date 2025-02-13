using MeLi.UrlShortener.Application.Cache;
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
        private readonly IUrlCache _urlCache;
        private readonly IUrlAnalyticsService _analyticsService;
        private readonly GeneralConfig _generalConfig;

        public UrlService(
            IUrlRepository urlRepository,
            IShortCodeGenerator codeGenerator,
            IUrlValidator urlValidator,
            IUrlCache urlCache,
            IUrlAnalyticsService analyticsService,
            IOptions<GeneralConfig> generalConfig)
        {
            _urlRepository = urlRepository ?? throw new ArgumentNullException(nameof(urlRepository));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _urlValidator = urlValidator ?? throw new ArgumentNullException(nameof(urlValidator));
            _urlCache = urlCache ?? throw new ArgumentNullException(nameof(urlCache));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _generalConfig = generalConfig?.Value ?? throw new ArgumentNullException(nameof(generalConfig));
        }

        public async Task<string> CreateShortUrlAsync(CreateShortUrlRequest request)
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

            return shortCode;
        }

        public async Task<string> GetLongUrlAsync(string shortCode)
        {
            // Try get from cache first
            var cachedUrl = await _urlCache.GetLongUrlAsync(shortCode);
            if (!string.IsNullOrEmpty(cachedUrl))
            {
                // Record analytics asynchronously without waiting
                _ = _analyticsService.RecordAccessAsync(shortCode);
                return cachedUrl;
            }

            // If not in cache, get from repository
            var urlEntity = await _urlRepository.GetByShortCodeAsync(shortCode)
                ?? throw new KeyNotFoundException("Short URL not found");

            // Cache the result for future requests
            await _urlCache.SetUrlAsync(shortCode, urlEntity.LongUrl.Value);

            // Record analytics asynchronously
            _ = _analyticsService.RecordAccessAsync(shortCode);

            return urlEntity.LongUrl.Value;
        }

        public async Task<bool> DeleteUrlAsync(string shortCode)
        {
            await _urlCache.DeleteLongUrlAsync(shortCode);
            return await _urlRepository.DeleteAsync(shortCode);
        }
    }
}