using CleanCut.Application.Enums;
using CleanCut.Application.Models;

namespace CleanCut.Application.Interfaces;

public interface IBackgroundRemovalService
{
    Task<ProcessedImageResult> RemoveBackgroundAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        OutputFormat outputFormat,
        string? backgroundColor,
        int quality,
        CancellationToken cancellationToken = default);
}
