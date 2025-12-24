using CleanCut.Application.Enums;
using CleanCut.Application.Exceptions;
using CleanCut.Application.Interfaces;
using CleanCut.Application.Models;
using CleanCut.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CleanCut.Infrastructure.Services;

public class BackgroundRemovalService : IBackgroundRemovalService, IDisposable
{
    private readonly ILogger<BackgroundRemovalService> _logger;
    private readonly ModelOptions _modelOptions;
    private readonly InferenceSession _session;
    private readonly string _outputName;
    private readonly string _inputName;
    private bool _disposed;

    private static readonly string[] AllowedContentTypes =
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp"
    };

    public BackgroundRemovalService(IOptions<ModelOptions> modelOptions, ILogger<BackgroundRemovalService> logger)
    {
        _logger = logger;
        _modelOptions = modelOptions.Value;

        if (!File.Exists(_modelOptions.ModelPath))
        {
            throw new FileNotFoundException("Model file not found", _modelOptions.ModelPath);
        }

        var options = new SessionOptions();
        options.AppendExecutionProvider_CPU();
        _session = new InferenceSession(_modelOptions.ModelPath, options);
        _outputName = _session.OutputMetadata.Keys.First();
        _inputName = _session.InputMetadata.Keys.First();
    }

    public async Task<ProcessedImageResult> RemoveBackgroundAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        OutputFormat outputFormat,
        string? backgroundColor,
        int quality,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ValidationException("Unsupported file type.");
        }

        using var image = await LoadImageAsync(fileStream, cancellationToken);

        var mask = await RunInferenceAsync(image, cancellationToken);
        var normalizedMask = NormalizeMask(mask);
        using var maskImage = CreateMaskImage(normalizedMask);
        maskImage.Mutate(x => x.Resize(image.Width, image.Height, KnownResamplers.Bicubic));

        return outputFormat == OutputFormat.Png
            ? await CreatePngAsync(image, maskImage, fileName, cancellationToken)
            : await CreateJpegAsync(image, maskImage, fileName, backgroundColor, quality, cancellationToken);
    }

    private static async Task<Image<Rgba32>> LoadImageAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            return await Image.LoadAsync<Rgba32>(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ValidationException($"Failed to read image: {ex.Message}");
        }
    }

    private async Task<float[]> RunInferenceAsync(Image<Rgba32> image, CancellationToken cancellationToken)
    {
        const int size = 320;
        using var resized = image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(size, size),
            Mode = ResizeMode.Crop,
            Sampler = KnownResamplers.Bicubic
        }));

        var input = new DenseTensor<float>(new[] { 1, 3, size, size });
        for (var y = 0; y < size; y++)
        {
            var span = resized.DangerousGetPixelRowMemory(y).Span;
            for (var x = 0; x < size; x++)
            {
                var pixel = span[x];
                var index = y * size + x;
                input[0, 0, y, x] = pixel.R / 255f;
                input[0, 1, y, x] = pixel.G / 255f;
                input[0, 2, y, x] = pixel.B / 255f;
            }
        }

        using var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, input)
        };

        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);
        var output = results.First(x => x.Name == _outputName);
        var tensor = (DenseTensor<float>)output.Value!;
        var mask = tensor.ToArray();
        return await Task.FromResult(mask);
    }

    private static float[] NormalizeMask(float[] mask)
    {
        var min = mask.Min();
        var max = mask.Max();
        var range = Math.Max(max - min, 1e-6f);
        return mask.Select(v => Math.Clamp((v - min) / range, 0f, 1f)).ToArray();
    }

    private static Image<L8> CreateMaskImage(float[] mask)
    {
        const int size = 320;
        var image = new Image<L8>(size, size);
        for (var y = 0; y < size; y++)
        {
            var row = image.DangerousGetPixelRowMemory(y).Span;
            for (var x = 0; x < size; x++)
            {
                var value = (byte)(mask[y * size + x] * 255);
                row[x] = new L8(value);
            }
        }

        return image;
    }

    private static Rgba32 ParseBackgroundColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Color.White;
        }

        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value))
        {
            throw new ValidationException("Background color must be a 6-character hex value (e.g., #ffffff).");
        }

        var r = (byte)((value >> 16) & 0xFF);
        var g = (byte)((value >> 8) & 0xFF);
        var b = (byte)(value & 0xFF);
        return new Rgba32(r, g, b);
    }

    private static async Task<ProcessedImageResult> CreatePngAsync(
        Image<Rgba32> source,
        Image<L8> mask,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var clone = source.Clone();
        for (var y = 0; y < clone.Height; y++)
        {
            var pixelRow = clone.DangerousGetPixelRowMemory(y).Span;
            var maskRow = mask.DangerousGetPixelRowMemory(y).Span;
            for (var x = 0; x < clone.Width; x++)
            {
                var alpha = maskRow[x].PackedValue;
                var pixel = pixelRow[x];
                pixel.A = alpha;
                pixelRow[x] = pixel;
            }
        }

        await using var ms = new MemoryStream();
        await clone.SaveAsPngAsync(ms, cancellationToken);
        return new ProcessedImageResult(ms.ToArray(), "image/png", Path.ChangeExtension(fileName, ".png"));
    }

    private static async Task<ProcessedImageResult> CreateJpegAsync(
        Image<Rgba32> source,
        Image<L8> mask,
        string fileName,
        string? backgroundColor,
        int quality,
        CancellationToken cancellationToken)
    {
        var bg = ParseBackgroundColor(backgroundColor);
        using var composite = new Image<Rgba32>(source.Width, source.Height, bg);

        for (var y = 0; y < source.Height; y++)
        {
            var srcRow = source.DangerousGetPixelRowMemory(y).Span;
            var maskRow = mask.DangerousGetPixelRowMemory(y).Span;
            var destRow = composite.DangerousGetPixelRowMemory(y).Span;
            for (var x = 0; x < source.Width; x++)
            {
                var alpha = maskRow[x].PackedValue / 255f;
                var srcPixel = srcRow[x];
                var dest = destRow[x];
                dest.R = (byte)(srcPixel.R * alpha + bg.R * (1 - alpha));
                dest.G = (byte)(srcPixel.G * alpha + bg.G * (1 - alpha));
                dest.B = (byte)(srcPixel.B * alpha + bg.B * (1 - alpha));
                destRow[x] = dest;
            }
        }

        var encoder = new JpegEncoder { Quality = Math.Clamp(quality, 0, 100) };
        await using var ms = new MemoryStream();
        await composite.SaveAsJpegAsync(ms, encoder, cancellationToken);
        return new ProcessedImageResult(ms.ToArray(), "image/jpeg", Path.ChangeExtension(fileName, ".jpg"));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _disposed = true;
    }
}
