using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SkiaSharp;

namespace El1teSpr1ntTrack.Infrastructure.Media;

public sealed class LegacyPhotoOptions
{
    public const string ContainerName = "historical-photos";
    public bool Enabled { get; init; }
}

public interface ILegacyPhotoBlobs
{
    Task<Stream?> ReadAsync(string hash, bool preview, CancellationToken token);
    Task PutIfAbsentAsync(string hash, bool preview, byte[] bytes, CancellationToken token);
}

public sealed class LegacyPhotoBlobs(Uri serviceUri, TokenCredential credential) : ILegacyPhotoBlobs
{
    private readonly BlobContainerClient container = new BlobServiceClient(serviceUri, credential)
        .GetBlobContainerClient(LegacyPhotoOptions.ContainerName);

    private BlobClient Blob(string hash, bool preview)
    {
        if (hash.Length != 64 || !hash.All(char.IsAsciiHexDigitLower))
            throw new ArgumentException("Invalid archive content key.");
        return container.GetBlobClient(preview ? $"preview/{hash}.webp" : $"original/{hash}");
    }

    public async Task<Stream?> ReadAsync(string hash, bool preview, CancellationToken token)
    {
        try { return (await Blob(hash, preview).DownloadStreamingAsync(cancellationToken: token)).Value.Content; }
        catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
    }

    public async Task PutIfAbsentAsync(string hash, bool preview, byte[] bytes, CancellationToken token)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        try
        {
            await Blob(hash, preview).UploadAsync(stream, new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
                HttpHeaders = new BlobHttpHeaders { ContentType = preview ? "image/webp" : "application/octet-stream" }
            }, token);
        }
        catch (RequestFailedException ex) when (ex.Status == 412 ||
            (ex.Status == 409 && ex.ErrorCode == BlobErrorCode.BlobAlreadyExists.ToString())) { }
    }
}

public static class LegacyPhotoImages
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public const int MaxPreviewBytes = 128 * 1024;
    private const int MaxPixels = 16_000_000;

    public static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken token)
    {
        using var result = new MemoryStream();
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(), token)) != 0)
        {
            if (result.Length + read > MaxBytes) throw new InvalidDataException("file-too-large");
            result.Write(buffer, 0, read);
        }
        if (result.Length == 0) throw new InvalidDataException("empty-file");
        return result.ToArray();
    }

    public static (byte[] Preview, int Width, int Height) InspectAndPreview(byte[] source, string format, int width, int height)
    {
        using var bitmap = Decode(source, format, width, height);
        foreach (var edge in new[] { 480, 384, 320 })
        {
            using var resized = Resize(bitmap, edge, flatten: false);
            foreach (var quality in new[] { 82, 72, 62, 52 })
            {
                var bytes = Encode(resized, SKEncodedImageFormat.Webp, quality);
                if (bytes.Length <= MaxPreviewBytes) return (bytes, bitmap.Width, bitmap.Height);
            }
        }
        throw new InvalidDataException("preview-too-large");
    }

    public static byte[] SocialJpeg(byte[] source)
    {
        using var bitmap = Decode(source, null, null, null);
        using var resized = Resize(bitmap, 1600, flatten: true);
        return Encode(resized, SKEncodedImageFormat.Jpeg, 90);
    }

    private static SKBitmap Decode(byte[] source, string? format, int? width, int? height)
    {
        if (source.Length is 0 or > MaxBytes) throw new InvalidDataException("invalid-file-size");
        using var data = SKData.CreateCopy(source);
        using var codec = SKCodec.Create(data);
        // Skia can report zero frames for still images without an animation frame table.
        if (codec is null || codec.FrameCount > 1 ||
            codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new InvalidDataException("unsupported-image");
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > MaxPixels)
            throw new InvalidDataException("invalid-image-dimensions");
        if (format is not null && !format.Equals(codec.EncodedFormat.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("image-type-mismatch");
        if (width is not null && (width != info.Width || height != info.Height))
            throw new InvalidDataException("image-dimensions-mismatch");
        using var srgb = SKColorSpace.CreateSrgb();
        using var decoded = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul, srgb));
        if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success)
            throw new InvalidDataException("image-decode-failed");
        return SkiaMediaDerivativeGenerator.ApplyOrientation(decoded, codec.EncodedOrigin);
    }

    private static SKBitmap Resize(SKBitmap source, int edge, bool flatten)
    {
        var scale = Math.Min(1, edge / (double)Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888,
            flatten ? SKAlphaType.Opaque : SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(flatten ? SKColors.White : SKColors.Transparent);
        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        canvas.Flush();
        return bitmap;
    }

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality) ?? throw new InvalidDataException("image-encode-failed");
        return data.ToArray();
    }
}
