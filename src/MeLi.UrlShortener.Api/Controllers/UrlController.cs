using MeLi.UrlShortener.Application.DTOs;
using MeLi.UrlShortener.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeLi.UrlShortener.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrlController : ControllerBase
    {
        private readonly IUrlService _urlService;

        public UrlController(IUrlService urlService)
        {
            _urlService = urlService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(UrlResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateShortUrl([FromBody] CreateShortUrlRequest request)
        {
            try
            {
                var result = await _urlService.CreateShortUrlAsync(request);
                return CreatedAtAction(nameof(GetUrl), new { shortCode = result.ShortUrl.Split('/').Last() }, result);
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