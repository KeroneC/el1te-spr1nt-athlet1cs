using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;
using Microsoft.Extensions.Logging;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class MediaService(
    IMediaRepository repository,
    IMediaStorage storage,
    IImageInspector imageInspector,
    MediaStorageOptions options,
    IClock clock,
    ILogger<MediaService> logger) : IMediaService
{
    public async Task<PagedResultDto<AdminMediaAssetDto>> GetAsync(AdminMediaOptions optionsQuery, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, optionsQuery.Page);
        var size = Math.Clamp(optionsQuery.PageSize, 1, 100);
        var result = await repository.GetAsync(optionsQuery with { Page = page, PageSize = size }, cancellationToken);
        return new PagedResultDto<AdminMediaAssetDto>(result.Items.Select(Map).ToList(), page, size, result.TotalCount);
    }

    public async Task<AdminMediaAssetDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await repository.GetAsync(id, cancellationToken) ?? throw new CmsNotFoundException("Media asset", id));

    public async Task<AdminMediaAssetDto> UploadAsync(Stream stream, long length, string originalFileName,
        string contentType, string title, string altText, string? caption, Guid uploadedByUserId,
        CancellationToken cancellationToken)
    {
        var errors = ValidateMetadata(title, altText, caption);
        if (length <= 0) errors["File"] = ["Choose a non-empty image file."];
        if (length > options.MaxFileSizeBytes) errors["File"] = [$"The image must be {options.MaxFileSizeBytes / 1024 / 1024} MB or smaller."];
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);

        await using var buffer = new MemoryStream((int)Math.Min(length, options.MaxFileSizeBytes + 1));
        await stream.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length <= 0 || buffer.Length > options.MaxFileSizeBytes)
            throw new CmsRequestValidationException(new Dictionary<string, string[]> { ["File"] = ["The image is empty or exceeds the configured size limit."] });

        buffer.Position = 0;
        var image = imageInspector.Inspect(buffer, originalFileName, contentType);
        buffer.Position = 0;
        var stored = await storage.SaveAsync(buffer, image.Extension, cancellationToken);
        var asset = new MediaAsset
        {
            OriginalFileName = Path.GetFileName(originalFileName),
            StorageKey = stored.StorageKey,
            ContentType = image.ContentType,
            FileExtension = image.Extension,
            FileSizeBytes = buffer.Length,
            Width = image.Width,
            Height = image.Height,
            Title = title.Trim(),
            AltText = altText.Trim(),
            Caption = Clean(caption),
            UploadedByUserId = uploadedByUserId,
            CreatedAtUtc = clock.UtcNow,
            IsActive = true
        };
        asset.PublicUrl = $"{options.PublicBaseUrl.TrimEnd('/')}/media/{asset.Id}";

        try
        {
            await repository.AddAsync(asset, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            try { await storage.DeleteAsync(stored.StorageKey, cancellationToken); }
            catch (Exception exception) { logger.LogWarning(exception, "Failed to clean up media storage after metadata persistence failed."); }
            throw;
        }
        return Map(asset);
    }

    public async Task<AdminMediaAssetDto> UpdateAsync(Guid id, MediaMetadataUpdateDto request, CancellationToken cancellationToken)
    {
        var errors = ValidateMetadata(request.Title, request.AltText, request.Caption);
        if (errors.Count > 0) throw new CmsRequestValidationException(errors);
        var asset = await repository.GetAsync(id, cancellationToken) ?? throw new CmsNotFoundException("Media asset", id);
        asset.Title = request.Title.Trim();
        asset.AltText = request.AltText.Trim();
        asset.Caption = Clean(request.Caption);
        asset.IsActive = request.IsActive;
        asset.UpdatedAtUtc = clock.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return Map(asset);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var asset = await repository.GetAsync(id, cancellationToken) ?? throw new CmsNotFoundException("Media asset", id);
        if (await repository.IsReferencedAsync(id, asset.PublicUrl, cancellationToken))
            throw new CmsConflictException("This media asset is still used by CMS content or a gallery. Remove those references before deleting it.");

        repository.Delete(asset);
        await repository.SaveChangesAsync(cancellationToken);
        try { await storage.DeleteAsync(asset.StorageKey, cancellationToken); }
        catch (Exception exception) { logger.LogWarning(exception, "Media metadata was deleted but the stored file could not be removed for asset {MediaAssetId}.", id); }
    }

    public async Task<PublicMediaFile?> OpenPublicAsync(Guid id, CancellationToken cancellationToken)
    {
        var asset = await repository.GetActiveAsync(id, cancellationToken);
        if (asset is null) return null;
        var stream = await storage.OpenReadAsync(asset.StorageKey, cancellationToken);
        return stream is null ? null : new PublicMediaFile(stream, asset.ContentType, asset.FileSizeBytes);
    }

    private static Dictionary<string, string[]> ValidateMetadata(string title, string altText, string? caption)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(title)) errors[nameof(title)] = ["Title is required."];
        else if (title.Trim().Length > 200) errors[nameof(title)] = ["Title must be 200 characters or fewer."];
        if ((altText ?? string.Empty).Trim().Length > 500) errors[nameof(altText)] = ["Alt text must be 500 characters or fewer."];
        if (caption?.Trim().Length > 1000) errors[nameof(caption)] = ["Caption must be 1,000 characters or fewer."];
        return errors;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AdminMediaAssetDto Map(MediaAsset asset) => new(asset.Id, asset.OriginalFileName,
        asset.ContentType, asset.FileExtension, asset.FileSizeBytes, asset.Width, asset.Height,
        asset.Title, asset.AltText, asset.Caption, asset.PublicUrl, asset.IsActive,
        asset.CreatedAtUtc, asset.UpdatedAtUtc);
}
