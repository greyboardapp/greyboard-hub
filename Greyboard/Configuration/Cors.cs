using Greyboard.Core;

namespace Greyboard.Configuration;

public static class CorsConfiguration
{
    private static string CORS_NAME = "ClientPermission";

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CORS_NAME, policy =>
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