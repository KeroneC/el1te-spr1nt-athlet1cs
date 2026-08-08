using System.Net;
using El1teSpr1ntTrack.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class PrintifyCatalogImageImporter(
    HttpClient httpClient,
    IMediaService mediaService,
    ILogger<PrintifyCatalogImageImporter> logger) : IPrintifyCatalogImageImporter
{
    private const long MaximumDownloadBytes = 10 * 1024 * 1024;

    public async Task<Guid?> ImportAsync(
        PrintifyImageSnapshot image,
        string productName,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(image.SourceUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            IsUnsafeHost(uri.Host))
        {
            logger.LogWarning("Skipped a Printify image with an unsafe source URI.");
            return null;
        }

        try
        {
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Printify image download failed with status {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var contentLength = response.Content.Headers.ContentLength;
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                contentLength is <= 0 or > MaximumDownloadBytes)
            {
                logger.LogWarning("Skipped a Printify image with invalid content metadata.");
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var asset = await mediaService.UploadAsync(
                stream,
                contentLength.Value,
                SafeFileName(uri, contentType),
                contentType,
                $"{productName} Printify mockup",
                $"{productName} merchandise",
                "Imported from the connected Printify shop.",
                actorUserId,
                cancellationToken);
            return asset.Id;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Printify image download failed.");
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Printify image download timed out.");
            return null;
        }
    }

    private static bool IsUnsafeHost(string host)
    {
        var trusted = string.Equals(host, "images.printify.com", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(host, "images-api.printify.com", StringComparison.OrdinalIgnoreCase) ||
                      host.EndsWith(".images.printify.com", StringComparison.OrdinalIgnoreCase) ||
                      host.EndsWith(".images-api.printify.com", StringComparison.OrdinalIgnoreCase);
        if (!trusted || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var address))
        {
            return false;
        }

        return IPAddress.IsLoopback(address) ||
               address.Equals(IPAddress.Any) ||
               address.Equals(IPAddress.IPv6Any) ||
               address.IsIPv6LinkLocal ||
               address.IsIPv6SiteLocal;
    }

    private static string SafeFileName(Uri uri, string contentType)
    {
        var source = Path.GetFileName(uri.LocalPath);
        if (!string.IsNullOrWhiteSpace(source) && Path.HasExtension(source))
        {
            return source;
        }

        var extension = contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
        return $"printify-product-{Guid.NewGuid():N}{extension}";
    }
}
