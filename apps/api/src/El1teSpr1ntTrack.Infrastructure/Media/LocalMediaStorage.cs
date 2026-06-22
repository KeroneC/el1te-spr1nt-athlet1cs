using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Infrastructure.Media;

public sealed class LocalMediaStorage(MediaStorageOptions options) : IMediaStorage
{
    private readonly string _root = EnsureRoot(options.LocalRoot);

    public async Task<StoredMediaFile> SaveAsync(Stream stream, string extension, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var key = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await stream.CopyToAsync(output, cancellationToken);
        return new StoredMediaFile(key.Replace('\\', '/'));
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = Resolve(storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = Resolve(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult(File.Exists(Resolve(storageKey)));

    private string Resolve(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey))
            throw new InvalidOperationException("The media storage key is invalid.");
        var path = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The media storage key is invalid.");
        return path;
    }

    private static string EnsureRoot(string root)
    {
        var full = Path.GetFullPath(root);
        Directory.CreateDirectory(full);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
