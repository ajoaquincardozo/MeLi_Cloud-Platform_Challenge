using MeLi.UrlShortener.Domain.ValueObjects;

namespace MeLi.UrlShortener.Domain.Entities
{
    public class UrlEntity
    {
        public string Id { get; private set; }
        private string LongUrlString { get; set; }
        public Url LongUrl => Url.Create(LongUrlString);

        private string ShortCodeString { get; set; }
        public ShortCode ShortCode => ShortCode.Create(ShortCodeString);

        public DateTime CreatedAt { get; private set; }
        public DateTime? LastAccessedAt { get; private set; }
        public long AccessCount { get; private set; }
        public bool IsActive { get; private set; }
        public string? CreatedBy { get; private set; }

        private DateTime? ExpirationDateValue { get; set; }
        public ExpirationDate ExpiresAt => ExpirationDate.Create(ExpirationDateValue);

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
                LongUrlString = url.Value,
                ShortCodeString = code.Value,
                CreatedAt = DateTime.UtcNow,
                AccessCount = 0,
                IsActive = true,
                CreatedBy = createdBy,
                ExpirationDateValue = expiration.Value
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
            ExpirationDateValue = ExpirationDate.Create(newExpirationDate).Value;
        }

        public bool IsExpired() => ExpiresAt.IsExpired();

        public bool CanBeAccessed() => IsActive && !IsExpired();

        public string GetFullShortUrl(string baseUrl = "https://me.li")
        {
            return $"{baseUrl.TrimEnd('/')}/{ShortCodeString}";
        }
    }
}