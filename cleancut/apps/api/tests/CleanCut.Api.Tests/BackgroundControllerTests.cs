using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CleanCut.Api;
using CleanCut.Application.Enums;
using CleanCut.Application.Interfaces;
using CleanCut.Application.Models;
using CleanCut.Infrastructure.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CleanCut.Api.Tests;

public class BackgroundControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BackgroundControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Invalid_file_returns_400()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not-an-image"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/background/remove", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Png_output_returns_image_png()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("placeholder"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "sample.png");

        var response = await _client.PostAsync("/api/background/remove?output=png", content);

        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [Fact]
    public async Task Too_large_file_returns_413()
    {
        using var content = new MultipartFormDataContent();
        var oversized = new byte[11 * 1024 * 1024];
        var fileContent = new ByteArrayContent(oversized);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "big.png");

        var response = await _client.PostAsync("/api/background/remove", content);

        response.StatusCode.Should().Be((HttpStatusCode)StatusCodes.Status413PayloadTooLarge);
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBackgroundRemovalService>();
            services.AddSingleton<IBackgroundRemovalService, FakeBackgroundRemovalService>();
            services.Configure<ApiOptions>(options =>
            {
                options.MaxFileSizeBytes = 10 * 1024 * 1024;
            });
            services.Configure<ModelOptions>(options => options.SkipDownload = true);
        });

        return base.CreateHost(builder);
    }
}

public class FakeBackgroundRemovalService : IBackgroundRemovalService
{
    private static readonly byte[] TransparentPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9YmmAoMAAAAASUVORK5CYII=");

    public Task<ProcessedImageResult> RemoveBackgroundAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        OutputFormat outputFormat,
        string? backgroundColor,
        int quality,
        CancellationToken cancellationToken = default)
    {
        if (!contentType.StartsWith("image"))
        {
            throw new CleanCut.Application.Exceptions.ValidationException("Unsupported file type.");
        }

        var result = new ProcessedImageResult(TransparentPng, "image/png", "processed.png");
        return Task.FromResult(result);
    }
}
