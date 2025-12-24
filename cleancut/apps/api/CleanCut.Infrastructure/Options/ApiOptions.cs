namespace CleanCut.Infrastructure.Options;

public class ApiOptions
{
    public const string SectionName = "Api";

    public string[] AllowedOrigins { get; set; } = ["http://localhost:3000"]; 

    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}
