using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Infrastructure.Media;
using SkiaSharp;

namespace El1teSpr1ntTrack.UnitTests;

public sealed class MediaValidationTests
{
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, ".jpg", "image/jpeg")]
    [InlineData(SKEncodedImageFormat.Png, ".png", "image/png")]
    [InlineData(SKEncodedImageFormat.Webp, ".webp", "image/webp")]
    public void Inspector_AcceptsSupportedDecodableImages(SKEncodedImageFormat format, string extension, string contentType)
    {
        using var bitmap = new SKBitmap(3, 2);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, 90);
        using var stream = new MemoryStream(encoded.ToArray());

        var result = new SkiaImageInspector().Inspect(stream, $"sample{extension}", contentType);

        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(3, result.Width);
        Assert.Equal(2, result.Height);
    }

    [Fact]
    public void Inspector_RejectsExtensionAndContentTypeMismatch()
    {
        using var stream = new MemoryStream([0x89, 0x50, 0x4e, 0x47]);
        Assert.Throws<CmsRequestValidationException>(() =>
            new SkiaImageInspector().Inspect(stream, "sample.png", "image/jpeg"));
    }

    [Fact]
    public void Inspector_RejectsCorruptImage()
    {
        using var stream = new MemoryStream([0xff, 0xd8, 0xff, 0x00]);
        Assert.Throws<CmsRequestValidationException>(() =>
            new SkiaImageInspector().Inspect(stream, "sample.jpg", "image/jpeg"));
    }

    [Fact]
    public async Task LocalStorage_GeneratesKeyAndBlocksTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), $"el1te-media-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalMediaStorage(new MediaStorageOptions { LocalRoot = root });
            await using var content = new MemoryStream([1, 2, 3]);
            var stored = await storage.SaveAsync(content, ".png", CancellationToken.None);

            Assert.Matches(@"^\d{4}/\d{2}/[a-f0-9]{32}\.png$", stored.StorageKey);
            Assert.True(await storage.ExistsAsync(stored.StorageKey, CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.ExistsAsync("../outside.png", CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
