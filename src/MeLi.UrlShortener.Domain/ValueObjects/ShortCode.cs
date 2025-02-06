namespace MeLi.UrlShortener.Domain.ValueObjects
{
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
}