namespace MeLi.UrlShortener.Application.DTOs
{
    public record UrlResponseDto
    {
        public string ShortUrl { get; init; }

        public UrlResponseDto(string shortUrl)
        {
            ShortUrl = shortUrl;
        }
    }
}
