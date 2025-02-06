using MeLi.UrlShortener.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace MeLi.UrlShortener.Domain.Entities
{
    public class UrlEntity
    {
        [Key]
        public string Id { get; private set; }

        private string _longUrlString;
        public Url LongUrl => Url.Create(_longUrlString);

        private string _shortCodeString;
        public ShortCode ShortCode => ShortCode.Create(_shortCodeString);

        public DateTime CreatedAt { get; private set; }
        public DateTime? LastAccessedAt { get; private set; }
        public long AccessCount { get; private set; }
        public bool IsActive { get; private set; }
        public string? CreatedBy { get; private set; }

        private DateTime? _expirationDateValue;
        public ExpirationDate ExpiresAt => ExpirationDate.Create(_expirationDateValue);

        protected UrlEntity() { }

        public static UrlEntity Create(
            string longUrl,
            string shortCode,
            string? createdBy = null,
            DateTime? expiresAt = null)
        {
            var url = Url.Create(longUrl);
            var code = ShortCode.Create(shortCode);
            var expiration = ExpirationDate.Create(expiresAt);

            return new UrlEntity
            {
                Id = Guid.NewGuid().ToString(),
                _longUrlString = url.Value,
                _shortCodeString = code.Value,
                CreatedAt = DateTime.UtcNow,
                AccessCount = 0,
                IsActive = true,
                CreatedBy = createdBy,
                _expirationDateValue = expiration.Value
            };
        }

        public void IncrementAccessCount()
        {
            if (!CanBeAccessed())
                throw new InvalidOperationException("Cannot increment access count for inactive or expired URL");

            AccessCount++;
            LastAccessedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void UpdateExpirationDate(DateTime newExpirationDate)
        {
            _expirationDateValue = newExpirationDate;
        }

        public bool IsExpired() => ExpiresAt.IsExpired();

        public bool CanBeAccessed() => IsActive && !IsExpired();

        public string GetFullShortUrl(string baseUrl = "https://me.li")
        {
            return $"{baseUrl.TrimEnd('/')}/{ShortCode}";
        }
    }
}