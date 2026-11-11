using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.LegacyPhotoImport;

public sealed record PhotoPage(string PageUrl);
public sealed record PhotoSource(string SourceUrl, List<PhotoPage> Pages, string Status, string OriginalFileName,
    string ArchivePath, long ByteLength, string Sha256, string Format, int Width, int Height);
public sealed record PhotoGroup(string Hash, List<PhotoSource> Sources);

public sealed class PhotoManifest(string root, IReadOnlyList<PhotoGroup> groups)
{
    private const string Site = "https://www.el1tespr1ntathlet1cs.org";
    private sealed record ManifestFile(int SchemaVersion, string SourceSite, List<PhotoSource> Items);
    public IReadOnlyList<PhotoGroup> Groups { get; } = groups;

    public static async Task<PhotoManifest> LoadAsync(string manifestPath, string checksum, CancellationToken token)
    {
        var path = Path.GetFullPath(manifestPath);
        await using var input = File.OpenRead(path);
        var bytes = await LegacyPhotoImages.ReadBoundedAsync(input, token);
        if (Hash(bytes) != checksum) throw new InvalidDataException("manifest-checksum-mismatch");
        var file = JsonSerializer.Deserialize<ManifestFile>(bytes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (file?.SchemaVersion != 1 || file.SourceSite != Site || file.Items is null || file.Items.Count == 0)
            throw new InvalidDataException("invalid-manifest");
        var sourceUrls = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in file.Items)
        {
            if (item.Status != "archived" || item.Sha256?.Length != 64 || !item.Sha256.All(char.IsAsciiHexDigitLower) ||
                !ValidHttps(item.SourceUrl, out _) || !sourceUrls.Add(item.SourceUrl) ||
                string.IsNullOrWhiteSpace(item.OriginalFileName) || item.OriginalFileName.Length > 255 ||
                item.ByteLength is <= 0 or > LegacyPhotoImages.MaxBytes || item.Width <= 0 || item.Height <= 0 ||
                item.Format is not ("jpeg" or "png" or "webp") || item.Pages is null || item.Pages.Count == 0 ||
                item.Pages.Any(page => !ValidHttps(page.PageUrl, out var uri) || uri.Host != new Uri(Site).Host ||
                    Collection(page.PageUrl).Length > 450))
                throw new InvalidDataException("invalid-manifest-item");
            ValidateArchivePath(item.ArchivePath);
        }
        var groups = file.Items.GroupBy(item => item.Sha256, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new PhotoGroup(group.Key, group.OrderBy(item => item.SourceUrl, StringComparer.Ordinal).ToList()))
            .ToList();
        return new PhotoManifest(Path.GetDirectoryName(path)!, groups);
    }

    public IReadOnlyList<PhotoGroup> SelectPilot()
    {
        var chosen = new List<PhotoGroup>();
        void Pick(Func<PhotoGroup, bool> predicate)
        {
            var item = Groups.FirstOrDefault(group => !chosen.Contains(group) && predicate(group));
            if (item is not null) chosen.Add(item);
        }
        Pick(group => group.Sources.Count > 1);
        Pick(group => group.Sources.Any(source => source.Pages.Count > 1));
        Pick(group => group.Sources[0].Format == "png");
        Pick(group => group.Sources[0].Format == "png");
        Pick(group => group.Sources[0] is { Format: "jpeg" } source && source.Width > source.Height);
        Pick(group => group.Sources[0] is { Format: "jpeg" } source && source.Height > source.Width);
        var collections = Groups.SelectMany(group => group.Sources.SelectMany(source => source.Pages.Select(page => (Key: Collection(page.PageUrl), Group: group))))
            .GroupBy(item => item.Key).OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new Queue<PhotoGroup>(group.Select(item => item.Group).Distinct())).ToList();
        while (chosen.Count < Math.Min(24, Groups.Count))
        {
            foreach (var queue in collections)
            {
                while (queue.TryDequeue(out var item))
                {
                    if (chosen.Contains(item)) continue;
                    chosen.Add(item);
                    break;
                }
                if (chosen.Count == Math.Min(24, Groups.Count)) break;
            }
        }
        return chosen;
    }

    public async Task<byte[]> ReadOriginalAsync(PhotoSource item, CancellationToken token)
    {
        var filename = ValidateArchivePath(item.ArchivePath);
        var directory = new DirectoryInfo(Path.Combine(root, "originals"));
        var file = new FileInfo(Path.Combine(directory.FullName, filename));
        if (directory.LinkTarget is not null || file.LinkTarget is not null)
            throw new InvalidDataException("symlink-not-allowed");
        if (!file.Exists) throw new InvalidDataException("missing-original");
        if (file.Length != item.ByteLength) throw new InvalidDataException("file-size-mismatch");
        await using var stream = file.OpenRead();
        var bytes = await LegacyPhotoImages.ReadBoundedAsync(stream, token);
        if (bytes.LongLength != item.ByteLength || Hash(bytes) != item.Sha256)
            throw new InvalidDataException("original-checksum-mismatch");
        return bytes;
    }

    private static string ValidateArchivePath(string path)
    {
        const string prefix = "originals/";
        if (path is null || !path.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidDataException("invalid-original-path");
        var name = path[prefix.Length..];
        if (name.Length == 0 || name is "." or ".." || name.IndexOfAny(['/', '\\', '\0']) >= 0)
            throw new InvalidDataException("invalid-original-path");
        return name;
    }

    private static bool ValidHttps(string value, out Uri uri)
    {
        if (value is { Length: <= 2048 } && Uri.TryCreate(value, UriKind.Absolute, out var parsed) &&
            parsed.Scheme == Uri.UriSchemeHttps && parsed.UserInfo.Length == 0)
        {
            uri = parsed;
            return true;
        }
        uri = null!;
        return false;
    }

    public static string Collection(string pageUrl) => new Uri(pageUrl).AbsolutePath.TrimEnd('/') is { Length: > 0 } path ? path : "/";
    public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    public static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
}

public sealed class ImportResult
{
    public int Verified { get; set; }
    public int Created { get; set; }
    public int Resumed { get; set; }
    public int Sources { get; set; }
    public int PageLinks { get; set; }
    public int Failed { get; set; }
    public long PreviewBytes { get; set; }
}

public sealed class PhotoImport(El1teDbContext db, ILegacyPhotoBlobs blobs)
{
    public static async Task RunAsync(PhotoManifest manifest, IReadOnlyList<PhotoGroup> groups, ImportResult result,
        PhotoImport? writer, CancellationToken token)
    {
        foreach (var group in groups)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var first = group.Sources[0];
                var original = await manifest.ReadOriginalAsync(first, token);
                foreach (var duplicate in group.Sources.Skip(1))
                {
                    if (duplicate.Width != first.Width || duplicate.Height != first.Height || duplicate.Format != first.Format)
                        throw new InvalidDataException("inconsistent-duplicate");
                    await manifest.ReadOriginalAsync(duplicate, token);
                }
                var (preview, width, height) = LegacyPhotoImages.InspectAndPreview(original, first.Format, first.Width, first.Height);
                if (writer is not null)
                {
                    if (await writer.PersistAsync(group, original, preview, width, height, token)) result.Created++;
                    else result.Resumed++;
                }
                result.Verified++;
                result.Sources += group.Sources.Count;
                result.PageLinks += group.Sources.Sum(source => source.Pages.Select(page => page.PageUrl).Distinct(StringComparer.Ordinal).Count());
                result.PreviewBytes += preview.Length;
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                result.Failed++;
                var reason = error is InvalidDataException ? error.Message : error.GetType().Name;
                Console.Error.WriteLine($"failed sha256={group.Hash} reason={reason}");
            }
            finally { writer?.ClearCheckpoint(); }
        }
    }

    private void ClearCheckpoint() => db.ChangeTracker.Clear();

    public async Task<bool> PersistAsync(PhotoGroup group, byte[] original, byte[] preview, int width, int height, CancellationToken token)
    {
        var photo = await db.LegacyPhotos.Include(item => item.Sources).ThenInclude(source => source.Pages)
            .SingleOrDefaultAsync(item => item.Sha256 == group.Hash, token);
        var created = photo is null;
        photo ??= new LegacyPhoto
        {
            Sha256 = group.Hash, ByteLength = original.LongLength, Width = width, Height = height, ImportedAtUtc = DateTimeOffset.UtcNow
        };
        foreach (var entry in group.Sources)
        {
            var key = PhotoManifest.Hash(entry.SourceUrl);
            var source = photo.Sources.SingleOrDefault(item => item.UrlSha256 == key);
            if (source is null)
            {
                if (await db.LegacyPhotoSources.AnyAsync(item => item.UrlSha256 == key, token))
                    throw new InvalidDataException("source-already-linked-to-different-content");
                source = new LegacyPhotoSource { UrlSha256 = key, LegacyPhotoId = photo.Id, SourceUrl = entry.SourceUrl, OriginalFileName = entry.OriginalFileName };
                photo.Sources.Add(source);
                if (!created) db.LegacyPhotoSources.Add(source);
            }
            foreach (var page in entry.Pages)
            {
                var pageHash = PhotoManifest.Hash(page.PageUrl);
                if (source.Pages.Any(item => item.PageUrlSha256 == pageHash)) continue;
                var link = new LegacyPhotoPage { SourceUrlSha256 = key, PageUrlSha256 = pageHash, PageUrl = page.PageUrl, Collection = PhotoManifest.Collection(page.PageUrl) };
                source.Pages.Add(link);
                if (!created) db.LegacyPhotoPages.Add(link);
            }
        }

        await SaveVerifiedAsync(group.Hash, original, preview: false, token);
        await SaveVerifiedAsync(group.Hash, preview, preview: true, token);
        if (created) db.LegacyPhotos.Add(photo);
        await db.SaveChangesAsync(token);
        return created;
    }

    private async Task SaveVerifiedAsync(string key, byte[] bytes, bool preview, CancellationToken token)
    {
        await blobs.PutIfAbsentAsync(key, preview, bytes, token);
        await using var stored = await blobs.ReadAsync(key, preview, token);
        if (stored is null) throw new InvalidDataException("blob-missing-after-write");
        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stored, token));
        if (hash != (preview ? PhotoManifest.Hash(bytes) : key))
            throw new InvalidDataException(preview ? "preview-blob-checksum-mismatch" : "original-blob-checksum-mismatch");
    }
}
