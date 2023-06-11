using Greyboard.Core;

namespace Greyboard.Configuration;

public static class CorsConfiguration
{
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, string name)
    {
        var clientUrl = services.BuildServiceProvider().GetService<AppSettings>()?.CLIENT_URLS.Split(";") ?? new[] { "http://localhost:3000" };

        services.AddCors(options =>
        {
            options.AddPolicy(name, policy =>
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithOrigins(clientUrl)
                    .AllowCredentials();
            });
        });
        return services;
    }
}