namespace MeLi.UrlShortener.Domain.Exceptions
{
    public class UrlShortenerException : Exception
    {
        public UrlShortenerException(string message) : base(message) { }
        public UrlShortenerException(string message, Exception inner) : base(message, inner) { }
    }

    public class UrlNotFoundException : UrlShortenerException
    {
        public UrlNotFoundException(string shortCode)
            : base($"URL with short code '{shortCode}' was not found") { }
    }

    public class UrlExpiredException : UrlShortenerException
    {
        public UrlExpiredException(string shortCode)
            : base($"URL with short code '{shortCode}' has expired") { }
    }

    public class InvalidUrlException : UrlShortenerException
    {
        public InvalidUrlException(string message) : base(message) { }
    }
}