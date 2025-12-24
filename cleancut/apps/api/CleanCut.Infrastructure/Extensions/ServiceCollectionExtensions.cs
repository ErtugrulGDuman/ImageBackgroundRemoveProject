using CleanCut.Application.Interfaces;
using CleanCut.Infrastructure.Options;
using CleanCut.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCut.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ModelOptions>(configuration.GetSection(ModelOptions.SectionName));
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));

        services.AddMemoryCache();
        services.AddHttpClient();

        services.AddSingleton<IBackgroundRemovalService, BackgroundRemovalService>();
        services.AddSingleton<IModelManager, ModelManager>();

        return services;
    }
}
