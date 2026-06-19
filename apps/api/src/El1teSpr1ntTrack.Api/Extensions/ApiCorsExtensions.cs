namespace El1teSpr1ntTrack.Api.Extensions;

public static class ApiCorsExtensions
{
    public const string DevelopmentCorsPolicy = "DevelopmentCorsPolicy";

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://127.0.0.1:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy(DevelopmentCorsPolicy, policy =>
            {
                // TODO: Replace development origins with explicit production domains before launch.
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
