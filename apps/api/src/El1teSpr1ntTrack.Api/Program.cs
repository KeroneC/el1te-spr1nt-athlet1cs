using System.Text.Json.Serialization;
using El1teSpr1ntTrack.Api.Authorization;
using El1teSpr1ntTrack.Api.Configuration;
using El1teSpr1ntTrack.Api.Extensions;
using El1teSpr1ntTrack.Api.Health;
using El1teSpr1ntTrack.Api.Middleware;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.Interfaces.Repositories;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Repositories;
using El1teSpr1ntTrack.Infrastructure.Security;
using El1teSpr1ntTrack.Infrastructure.Media;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

ProductionConfigurationValidator.Validate(builder.Configuration, builder.Environment);

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
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddApiCors(builder.Configuration);

builder.Services.AddScoped<IClock, SystemClock>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISlugGenerator, SlugGenerator>();
builder.Services.AddScoped<ICmsValidationService, CmsValidationService>();
builder.Services.AddScoped<IPublicCmsService, PublicCmsService>();
builder.Services.AddScoped<IPublicCmsRepository, PublicCmsRepository>();
builder.Services.AddScoped<IAdminCmsService, AdminCmsService>();
builder.Services.AddScoped<IAdminCmsRepository, AdminCmsRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IGalleryService, GalleryService>();
builder.Services.AddScoped<IGalleryRepository, GalleryRepository>();
builder.Services.AddSingleton<IImageInspector, SkiaImageInspector>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(ICmsRepository<>), typeof(CmsRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveCmsAdminHandler>();
builder.Services.AddScoped<DevelopmentAdminSeeder>();
builder.Services.AddScoped<ProductionAdminBootstrapper>();

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

builder.Services.AddDbContext<El1teDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    });

builder.Services.AddAuthorization(CmsAdminAuthorization.Configure);

var app = builder.Build();

if (args.Contains("--bootstrap-admin", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var created = await scope.ServiceProvider.GetRequiredService<ProductionAdminBootstrapper>().RunAsync();
    Console.WriteLine(created ? "SuperAdmin created." : "Configured admin already exists; no changes made.");
    return;
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentAdminSeeder>().SeedAsync();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(ApiCorsExtensions.ConfiguredCorsPolicy);
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
app.MapControllers();

app.Run();

public partial class Program;
