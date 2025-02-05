using MeLi.UrlShortener.Application.Interfaces;

namespace MeLi.UrlShortener.Application.Services
{
    public class ShortCodeGenerator : IShortCodeGenerator
    {
        private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;
        private readonly Random _random;

        public ShortCodeGenerator()
        {
            _random = new Random();
        }

        public string GenerateCode()
        {
            var code = new char[CodeLength];
            for (int i = 0; i < CodeLength; i++)
            {
                code[i] = AllowedChars[_random.Next(AllowedChars.Length)];
            }
            return new string(code);
        }
    }
}
