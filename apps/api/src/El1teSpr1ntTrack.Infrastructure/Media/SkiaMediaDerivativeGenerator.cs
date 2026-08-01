using System.Security.Cryptography;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using SkiaSharp;

namespace El1teSpr1ntTrack.Infrastructure.Media;

public sealed class SkiaMediaDerivativeGenerator : IMediaDerivativeGenerator
{
    private static readonly int[] TargetWidths = [480, 960, 1600];

    public IReadOnlyList<GeneratedMediaDerivative> Generate(Stream source)
    {
        source.Position = 0;
        using var data = SKData.Create(source);
        using var codec = SKCodec.Create(data) ?? throw new InvalidOperationException("The uploaded image could not be decoded.");
        using var decoded = SKBitmap.Decode(codec) ?? throw new InvalidOperationException("The uploaded image could not be decoded.");
        using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);

        var results = new List<GeneratedMediaDerivative>();
        foreach (var requestedWidth in TargetWidths.Where(width => width <= oriented.Width))
        {
            var height = Math.Max(1, (int)Math.Round(oriented.Height * (requestedWidth / (double)oriented.Width)));
            using var resized = oriented.Resize(new SKImageInfo(requestedWidth, height, SKColorType.Rgba8888, SKAlphaType.Premul),
                new SKSamplingOptions(SKCubicResampler.Mitchell));
            if (resized is null) throw new InvalidOperationException("An optimized image derivative could not be generated.");
            using var image = SKImage.FromBitmap(resized);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, 82);
            var bytes = encoded.ToArray();
            results.Add(new GeneratedMediaDerivative(requestedWidth, requestedWidth, height, bytes,
                Convert.ToHexString(SHA256.HashData(bytes))));
        }

        return results;
    }

    internal static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsDimensions = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or
            SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var result = new SKBitmap(swapsDimensions ? source.Height : source.Width,
            swapsDimensions ? source.Width : source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(result);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Translate(source.Width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight:
                canvas.Translate(source.Width, source.Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, source.Height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(source.Height, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(source.Height, source.Width); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, source.Width); canvas.RotateDegrees(270); break;
        }

        canvas.DrawBitmap(source, 0, 0);
        canvas.Flush();
        return result;
    }
}
