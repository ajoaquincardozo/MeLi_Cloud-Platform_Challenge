using MeLi.UrlShortener.Application.DTOs;
using MeLi.UrlShortener.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace MeLi.UrlShortener.Api.Controllers
{
    [ApiController]
    [Route("/")]
    public class UrlController : ControllerBase
    {
        private readonly IUrlService _urlService;
        private readonly IUrlHelperFactory _urlHelperFactory;

        public UrlController(IUrlService urlService, IUrlHelperFactory urlHelperFactory)
        {
            _urlService = urlService ?? throw new ArgumentNullException(nameof(urlService));
            _urlHelperFactory = urlHelperFactory ?? throw new ArgumentNullException(nameof(urlHelperFactory));
        }

        [HttpPost]
        [ProducesResponseType(typeof(UrlResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateShortUrl([FromBody] CreateShortUrlRequest request)
        {
            try
            {
                var shortCode = await _urlService.CreateShortUrlAsync(request);
                var urlHelper = _urlHelperFactory.GetUrlHelper(ControllerContext);

                return CreatedAtAction(
                    nameof(GetUrl),
                    new { shortCode },
                    urlHelper.ActionLink(nameof(GetUrl), values: new { shortCode }));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{shortCode}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUrl(string shortCode)
        {
            try
            {
                var longUrl = await _urlService.GetLongUrlAsync(shortCode);
                return Redirect(longUrl);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{shortCode}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUrl(string shortCode)
        {
            var result = await _urlService.DeleteUrlAsync(shortCode);
            return result ? NoContent() : NotFound();
        }
    }
}