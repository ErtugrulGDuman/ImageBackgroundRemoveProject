using System.Collections.Concurrent;
using CleanCut.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace CleanCut.Infrastructure.Services;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;
    private static readonly ConcurrentDictionary<string, RateEntry> Entries = new();

    public RateLimitMiddleware(RequestDelegate next, IOptions<RateLimitOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        var now = DateTimeOffset.UtcNow;
        var window = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero);

        var entry = Entries.AddOrUpdate(key, _ => new RateEntry(window, 1), (_, existing) =>
        {
            if (existing.WindowStart == window)
            {
                existing.Count++;
            }
            else
            {
                existing = new RateEntry(window, 1);
            }

            return existing;
        });

        if (entry.WindowStart < window.AddMinutes(-1))
        {
            Entries.TryRemove(key, out _);
        }

        if (entry.Count > _options.RequestsPerMinute)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Please try again shortly." });
            return;
        }

        await _next(context);
    }

    private sealed record RateEntry(DateTimeOffset WindowStart, int Count);
}
