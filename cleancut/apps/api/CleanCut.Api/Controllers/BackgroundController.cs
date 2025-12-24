using CleanCut.Application.Enums;
using CleanCut.Application.Exceptions;
using CleanCut.Application.Interfaces;
using CleanCut.Application.Models;
using CleanCut.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CleanCut.Api.Controllers;

[ApiController]
[Route("api/background")]
public class BackgroundController : ControllerBase
{
    private readonly IBackgroundRemovalService _backgroundRemovalService;
    private readonly ApiOptions _apiOptions;

    public BackgroundController(IBackgroundRemovalService backgroundRemovalService, IOptions<ApiOptions> apiOptions)
    {
        _backgroundRemovalService = backgroundRemovalService;
        _apiOptions = apiOptions.Value;
    }

    [HttpPost("remove")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Remove(
        [FromForm] IFormFile file,
        [FromQuery] string output = "png",
        [FromQuery] string? bgColor = null,
        [FromQuery] int quality = 92,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required." });
        }

        if (file.Length > _apiOptions.MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "File exceeds 10MB limit." });
        }

        if (!Enum.TryParse<OutputFormat>(output, true, out var parsedOutput))
        {
            return BadRequest(new { error = "Output must be png or jpg." });
        }

        if (parsedOutput == OutputFormat.Jpeg && quality is < 0 or > 100)
        {
            return BadRequest(new { error = "Quality must be between 0 and 100." });
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        try
        {
            ProcessedImageResult result = await _backgroundRemovalService.RemoveBackgroundAsync(
                stream,
                file.FileName,
                file.ContentType ?? string.Empty,
                parsedOutput,
                parsedOutput == OutputFormat.Jpeg ? bgColor : null,
                quality,
                cancellationToken);

            return File(result.Data, result.ContentType, result.FileName);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
