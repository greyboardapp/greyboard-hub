using Greyboard.Core;

namespace Greyboard.Configuration;

public static class CorsConfiguration
{

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, string name)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(name, policy =>
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithOrigins(services.BuildServiceProvider().GetService<AppSettings>()?.CLIENT_URL ?? "http://localhost:3000")
                    .AllowCredentials();
            });
        });
        return services;
    }
}