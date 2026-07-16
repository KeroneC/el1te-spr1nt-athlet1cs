using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using SkiaSharp;

namespace El1teSpr1ntTrack.Infrastructure.Media;

public sealed class SkiaImageResizer : IImageResizer
{
    public PublicMediaFile Resize(Stream stream, string contentType, int width)
    {
        using var source = SKBitmap.Decode(stream) ?? throw new InvalidOperationException("The stored image could not be decoded.");
        var height = Math.Max(1, (int)Math.Round(source.Height * (width / (double)source.Width)));
        using var resized = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(resized);
        canvas.Clear(SKColors.Transparent);
        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, SKRect.Create(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell), null);
        canvas.Flush();

        var format = contentType switch
        {
            "image/png" => SKEncodedImageFormat.Png,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg
        };
        using var outputImage = SKImage.FromBitmap(resized);
        using var encoded = outputImage.Encode(format, 82);
        var bytes = encoded.ToArray();
        return new PublicMediaFile(new MemoryStream(bytes, writable: false), contentType, bytes.Length);
    }
}
