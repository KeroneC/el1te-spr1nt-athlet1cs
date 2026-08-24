using System.Text.Json.Serialization;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Api.Background;
using El1teSpr1ntTrack.Api.Configuration;
using El1teSpr1ntTrack.Api.Extensions;
using El1teSpr1ntTrack.Api.Health;
using El1teSpr1ntTrack.Api.Middleware;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.Interfaces.Repositories;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Commerce;
using El1teSpr1ntTrack.Infrastructure.Repositories;
using El1teSpr1ntTrack.Infrastructure.Security;
using El1teSpr1ntTrack.Infrastructure.Media;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var isAdminBootstrap = args.Contains("--bootstrap-admin", StringComparer.OrdinalIgnoreCase);
var isMediaBackfill = args.Contains("--backfill-media-derivatives", StringComparer.OrdinalIgnoreCase);

ProductionConfigurationValidator.Validate(
    builder.Configuration,
    builder.Environment,
    allowSqlPasswordAuthentication: isAdminBootstrap);

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ApplicationVersion = builder.Configuration["RELEASE_SHA"];
    });
}

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var seqUrl = context.Configuration["Serilog:SeqUrl"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        configuration.WriteTo.Seq(seqUrl);
    }
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT bearer token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            []
        }
    });
});
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<SquareHealthCheck>("square", tags: ["commerce"]);
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20, Window = TimeSpan.FromMinutes(15), QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("public-write", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 2;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<IClock, SystemClock>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminAuthenticationService, AdminAuthenticationService>();
builder.Services.AddScoped<IAdminIdentityService, AdminIdentityService>();
builder.Services.AddScoped<IAdminIdentityRepository, AdminIdentityRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISlugGenerator, SlugGenerator>();
builder.Services.AddScoped<ICmsValidationService, CmsValidationService>();
builder.Services.AddScoped<IPublicCmsService, PublicCmsService>();
builder.Services.AddScoped<IPublicCmsRepository, PublicCmsRepository>();
builder.Services.AddScoped<IAdminCmsService, AdminCmsService>();
builder.Services.AddScoped<IAdminCmsRepository, AdminCmsRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddSingleton<IMediaDerivativeGenerator, SkiaMediaDerivativeGenerator>();
builder.Services.AddScoped<MediaDerivativeBackfillService>();
builder.Services.AddScoped<IGalleryService, GalleryService>();
builder.Services.AddScoped<IGalleryRepository, GalleryRepository>();
builder.Services.AddScoped<IStoreAdminService, StoreAdminService>();
builder.Services.AddScoped<IPublicStoreService, PublicStoreService>();
builder.Services.AddScoped<IStoreOrderService, StoreOrderService>();
builder.Services.AddSingleton<IImageInspector, SkiaImageInspector>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(ICmsRepository<>), typeof(CmsRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveCmsAdminHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveSuperAdminHandler>();
builder.Services.AddScoped<DevelopmentAdminSeeder>();
builder.Services.AddScoped<ProductionAdminBootstrapper>();

var storeSettings = builder.Configuration
    .GetSection(StoreSettings.SectionName)
    .Get<StoreSettings>() ?? new StoreSettings();
var squareSettings = builder.Configuration
    .GetSection(SquareSettings.SectionName)
    .Get<SquareSettings>() ?? new SquareSettings();
builder.Services.AddSingleton(storeSettings);
builder.Services.AddSingleton(squareSettings);
builder.Services.AddSingleton<ISquareSignatureVerifier, SquareSignatureVerifier>();
builder.Services.AddScoped<ISquareWebhookService, SquareWebhookService>();
builder.Services.AddScoped<ICommerceOutboxProcessor, CommerceOutboxProcessor>();
builder.Services.AddHttpClient<ISquareClient, SquareClient>(client =>
{
    client.BaseAddress = new Uri(squareSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(squareSettings.RequestTimeoutSeconds, 5, 60));
});
builder.Services.AddHostedService<CommerceOutboxWorker>();

var authFeatureSettings = builder.Configuration
    .GetSection(AuthFeatureSettings.SectionName).Get<AuthFeatureSettings>() ?? new AuthFeatureSettings();
var transactionalEmailSettings = builder.Configuration
    .GetSection(TransactionalEmailSettings.SectionName).Get<TransactionalEmailSettings>() ?? new TransactionalEmailSettings();
if (!Path.IsPathRooted(transactionalEmailSettings.DevelopmentOutboxPath))
{
    transactionalEmailSettings.DevelopmentOutboxPath = Path.Combine(
        builder.Environment.ContentRootPath, transactionalEmailSettings.DevelopmentOutboxPath);
}
builder.Services.AddSingleton(authFeatureSettings);
builder.Services.AddSingleton(transactionalEmailSettings);
builder.Services.AddSingleton<ITransactionalEmailSender>(_ =>
    string.Equals(transactionalEmailSettings.Provider, "AzureCommunicationServices", StringComparison.OrdinalIgnoreCase)
        ? new AzureCommunicationEmailSender(transactionalEmailSettings)
        : new DevelopmentFileEmailSender(transactionalEmailSettings));

var adminInvitationSettings = builder.Configuration
    .GetSection(AdminInvitationSettings.SectionName)
    .Get<AdminInvitationSettings>() ?? new AdminInvitationSettings();
builder.Services.AddSingleton(adminInvitationSettings);

var mediaStorageOptions = builder.Configuration
    .GetSection(MediaStorageOptions.SectionName)
    .Get<MediaStorageOptions>() ?? new MediaStorageOptions();
if (string.Equals(mediaStorageOptions.Provider, "Local", StringComparison.OrdinalIgnoreCase) && !Path.IsPathRooted(mediaStorageOptions.LocalRoot))
{
    mediaStorageOptions.LocalRoot = Path.Combine(builder.Environment.ContentRootPath, mediaStorageOptions.LocalRoot);
}
builder.Services.AddSingleton(mediaStorageOptions);
builder.Services.AddSingleton<IMediaStorage>(provider =>
    string.Equals(mediaStorageOptions.Provider, "AzureBlob", StringComparison.OrdinalIgnoreCase)
        ? new AzureBlobMediaStorage(mediaStorageOptions)
        : new LocalMediaStorage(mediaStorageOptions));
builder.Services.AddHostedService<MediaDerivativeBackfillWorker>();

var databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useManagedIdentity = builder.Configuration.GetValue<bool>("Database:UseManagedIdentity");
builder.Services.AddDbContext<El1teDbContext>(options =>
{
    if (!useManagedIdentity)
    {
        options.UseSqlServer(databaseConnectionString);
        return;
    }

    var connection = new SqlConnection(databaseConnectionString)
    {
        AccessTokenCallback = AzureSqlAccessTokenProvider.Callback
    };
    options.UseSqlServer(connection, contextOwnsConnection: true);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = JwtSecurityKeyFactory.Create(builder.Configuration["Jwt:Key"]);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var identifier = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var versionClaim = context.Principal?.FindFirst("security_version")?.Value;
                if (!Guid.TryParse(identifier, out var userId) || !int.TryParse(versionClaim, out var tokenVersion))
                {
                    context.Fail("The session is no longer valid.");
                    return;
                }

                var repository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await repository.GetByIdAsync(userId, context.HttpContext.RequestAborted);
                if (user is null || !user.IsActive || user.SecurityVersion != tokenVersion)
                    context.Fail("The session is no longer valid.");
            }
        };
    });

builder.Services.AddAuthorization(CmsAdminAuthorization.Configure);

var app = builder.Build();

if (isAdminBootstrap)
{
    await using var scope = app.Services.CreateAsyncScope();
    var created = await scope.ServiceProvider.GetRequiredService<ProductionAdminBootstrapper>().RunAsync();
    Console.WriteLine(created ? "SuperAdmin created." : "Configured admin already exists; no changes made.");
    return;
}

if (isMediaBackfill)
{
    await using var scope = app.Services.CreateAsyncScope();
    var report = await scope.ServiceProvider.GetRequiredService<MediaDerivativeBackfillService>().RunAsync(
        cancellationToken: default);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    Environment.ExitCode = report.Failed == 0 ? 0 : 2;
    return;
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentAdminSeeder>().SeedAsync();
}

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseMiddleware<PrivacySafeRequestTelemetryMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    await next();
});
app.UseCors(ApiCorsExtensions.ConfiguredCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new()
{
    Predicate = registration => !registration.Tags.Contains("ready"),
    ResponseWriter = SafeHealthResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/ready", new()
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = SafeHealthResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/commerce", new()
{
    Predicate = registration => registration.Tags.Contains("commerce"),
    ResponseWriter = SafeHealthResponseWriter.WriteAsync
});
app.MapControllers();

app.Run();

public partial class Program;
