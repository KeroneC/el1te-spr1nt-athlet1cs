using System.Net;
using El1teSpr1ntTrack.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace El1teSpr1ntTrack.Infrastructure.Commerce;

public sealed class SquareCatalogImageImporter(
    HttpClient httpClient,
    IMediaService mediaService,
    ILogger<SquareCatalogImageImporter> logger) : ISquareCatalogImageImporter
{
    private const long MaximumDownloadBytes = 10 * 1024 * 1024;

    public async Task<Guid?> ImportAsync(
        SquareCatalogImage image,
        string productName,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(image.Url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            IsUnsafeHost(uri.Host))
        {
            logger.LogWarning("Skipped a Square catalog image with an unsafe source URI.");
            return null;
        }

        try
        {
            using var response = await httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Square catalog image download failed with status {StatusCode}.",
                    (int)response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Skipped a Square catalog image with a non-image content type.");
                return null;
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength is <= 0 or > MaximumDownloadBytes)
            {
                logger.LogWarning("Skipped a Square catalog image with an invalid content length.");
                return null;
            }
            var safeContentLength = contentLength.GetValueOrDefault();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var fileName = SafeFileName(uri, contentType);
            var asset = await mediaService.UploadAsync(
                stream,
                safeContentLength,
                fileName,
                contentType,
                $"{productName} product image",
                $"{productName} merchandise",
                image.Caption,
                actorUserId,
                cancellationToken);
            return asset.Id;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Square catalog image download failed.");
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Square catalog image download timed out.");
            return null;
        }
    }

    private static bool IsUnsafeHost(string host)
    {
        var trustedSquareHost =
            string.Equals(host, "squarecdn.com", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "squareup.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".squarecdn.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".squareup.com", StringComparison.OrdinalIgnoreCase) ||
            (host.StartsWith("items-images-production.", StringComparison.OrdinalIgnoreCase) &&
             host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase));
        if (!trustedSquareHost)
        {
            return true;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
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
               address.IsIPv6SiteLocal ||
               address.ToString().StartsWith("10.", StringComparison.Ordinal) ||
               address.ToString().StartsWith("192.168.", StringComparison.Ordinal) ||
               IsPrivate172(address);
    }

    private static bool IsPrivate172(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] == 172 && bytes[1] is >= 16 and <= 31;
    }

    private static string SafeFileName(Uri uri, string contentType)
    {
        var sourceName = Path.GetFileName(uri.LocalPath);
        if (!string.IsNullOrWhiteSpace(sourceName) && Path.HasExtension(sourceName))
        {
            return sourceName;
        }

        var extension = contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
        return $"square-product-{Guid.NewGuid():N}{extension}";
    }
}
