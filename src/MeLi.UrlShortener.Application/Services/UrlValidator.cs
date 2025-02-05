using MeLi.UrlShortener.Application.Interfaces;

namespace MeLi.UrlShortener.Application.Services
{
    public class UrlValidator : IUrlValidator
    {
        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
