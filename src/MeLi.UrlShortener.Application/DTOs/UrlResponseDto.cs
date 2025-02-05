namespace MeLi.UrlShortener.Application.DTOs
{
    public record UrlResponseDto
    {
        public string ShortUrl { get; init; }
        public string LongUrl { get; init; }
        public DateTime CreatedAt { get; init; }
        public long AccessCount { get; init; }

        public UrlResponseDto(string shortUrl, string longUrl, DateTime createdAt, long accessCount)
        {
            ShortUrl = shortUrl;
            LongUrl = longUrl;
            CreatedAt = createdAt;
            AccessCount = accessCount;
        }
    }
}
