using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using SkiaSharp;

namespace El1teSpr1ntTrack.Infrastructure.Media;

public sealed class SkiaImageInspector : IImageInspector
{
    private static readonly IReadOnlyDictionary<string, (string ContentType, SKEncodedImageFormat Format)> Supported =
        new Dictionary<string, (string, SKEncodedImageFormat)>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ("image/jpeg", SKEncodedImageFormat.Jpeg),
            [".jpeg"] = ("image/jpeg", SKEncodedImageFormat.Jpeg),
            [".png"] = ("image/png", SKEncodedImageFormat.Png),
            [".webp"] = ("image/webp", SKEncodedImageFormat.Webp)
        };

    public InspectedImage Inspect(Stream stream, string originalFileName, string declaredContentType)
    {
        var extension = Path.GetExtension(Path.GetFileName(originalFileName)).ToLowerInvariant();
        if (!Supported.TryGetValue(extension, out var expected) ||
            !string.Equals(expected.ContentType, declaredContentType, StringComparison.OrdinalIgnoreCase))
            throw Invalid("File", "Upload a JPEG, PNG, or WebP image with a matching file type.");

        try
        {
            stream.Position = 0;
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            var bytes = copy.ToArray();
            using var codecStream = new MemoryStream(bytes, writable: false);
            using var codec = SKCodec.Create(codecStream);
            if (codec is null || codec.EncodedFormat != expected.Format || codec.Info.Width <= 0 || codec.Info.Height <= 0)
                throw Invalid("File", "The image file is corrupt or does not match its declared format.");

            using var decodeStream = new MemoryStream(bytes, writable: false);
            using var bitmap = SKBitmap.Decode(decodeStream);
            if (bitmap is null || bitmap.Width != codec.Info.Width || bitmap.Height != codec.Info.Height)
                throw Invalid("File", "The image file could not be decoded safely.");

            stream.Position = 0;
            return new InspectedImage(expected.ContentType, extension == ".jpeg" ? ".jpg" : extension, bitmap.Width, bitmap.Height);
        }
        catch (CmsRequestValidationException) { throw; }
        catch
        {
            throw Invalid("File", "The image file is corrupt or could not be decoded.");
        }
    }

    private static CmsRequestValidationException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
