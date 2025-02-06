namespace MeLi.UrlShortener.Domain.ValueObjects
{
    public class Url
    {
        public string Value { get; }

        private Url(string value)
        {
            Value = value;
        }

        public static Url Create(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL cannot be empty", nameof(url));

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
                throw new ArgumentException("Invalid URL format", nameof(url));

            if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("URL must be HTTP or HTTPS", nameof(url));

            return new Url(url);
        }

        public override string ToString() => Value;
    }
}