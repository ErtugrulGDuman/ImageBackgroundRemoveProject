using System.Net.Http.Headers;
using CleanCut.Application.Interfaces;
using CleanCut.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanCut.Infrastructure.Services;

public class ModelManager : IModelManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModelManager> _logger;
    private readonly ModelOptions _options;

    public ModelManager(IHttpClientFactory httpClientFactory, IOptions<ModelOptions> options, ILogger<ModelManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task EnsureModelAsync(CancellationToken cancellationToken = default)
    {
        var modelPath = _options.ModelPath;
        if (File.Exists(modelPath))
        {
            _logger.LogInformation("Model already present at {Path}", modelPath);
            return;
        }

        if (_options.SkipDownload)
        {
            _logger.LogWarning("Model missing at {Path} but download is skipped by configuration.", modelPath);
            return;
        }

        var directory = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _logger.LogInformation("Downloading model from {Url} to {Path}", _options.ModelUrl, modelPath);
        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(_options.ModelUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(modelPath);
        await stream.CopyToAsync(fileStream, cancellationToken);
        _logger.LogInformation("Model downloaded successfully.");
    }
}
