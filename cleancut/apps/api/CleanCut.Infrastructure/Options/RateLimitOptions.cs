namespace CleanCut.Infrastructure.Options;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int RequestsPerMinute { get; set; } = 20;
}
