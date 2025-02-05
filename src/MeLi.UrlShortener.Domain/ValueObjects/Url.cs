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

    public class ShortCode
    {
        public string Value { get; }
        private const int MinLength = 6;
        private const int MaxLength = 10;
        private const string ValidCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private ShortCode(string value)
        {
            Value = value;
        }

        public static ShortCode Create(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Short code cannot be empty", nameof(code));

            if (code.Length < MinLength || code.Length > MaxLength)
                throw new ArgumentException($"Short code must be between {MinLength} and {MaxLength} characters", nameof(code));

            if (code.Any(c => !ValidCharacters.Contains(c)))
                throw new ArgumentException($"Short code contains invalid characters", nameof(code));

            return new ShortCode(code);
        }

        public static implicit operator string(ShortCode shortCode) => shortCode.Value;

        public override string ToString() => Value;
    }

    public class ExpirationDate
    {
        public DateTime? Value { get; }
        private const int MaxDaysInFuture = 365;

        private ExpirationDate(DateTime? value)
        {
            Value = value;
        }

        public static ExpirationDate Create(DateTime? date)
        {
            if (!date.HasValue)
                return new ExpirationDate(null);

            if (date.Value <= DateTime.UtcNow)
                throw new ArgumentException("Expiration date must be in the future", nameof(date));

            if (date.Value > DateTime.UtcNow.AddDays(MaxDaysInFuture))
                throw new ArgumentException($"Expiration date cannot be more than {MaxDaysInFuture} days in the future", nameof(date));

            return new ExpirationDate(date.Value);
        }

        public bool IsExpired() => Value.HasValue && Value.Value <= DateTime.UtcNow;
    }
}