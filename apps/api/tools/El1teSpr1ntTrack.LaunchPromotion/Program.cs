using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

var arguments = PromotionArguments.Parse(args);
if (arguments.SourceEnvironment.Equals(arguments.DestinationEnvironment, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Source and destination environments must be different.");

if (arguments.Command == "export")
{
    await using var source = CreateContext(arguments.SourceConnection);
    var manifest = await PromotionEngine.ExportAsync(source, arguments, CreateStorage(arguments, source: true));
    await File.WriteAllTextAsync(arguments.ManifestPath, JsonSerializer.Serialize(manifest, PromotionJson.Options));
    Console.WriteLine($"Review manifest written to {arguments.ManifestPath}. Included {manifest.Records.Count(value => value.Include)} of {manifest.Records.Count} eligible records.");
    return;
}

var input = JsonSerializer.Deserialize<PromotionManifest>(await File.ReadAllTextAsync(arguments.ManifestPath), PromotionJson.Options)
    ?? throw new InvalidOperationException("The manifest is invalid.");
PromotionEngine.Validate(input, arguments);
if (!arguments.Apply)
{
    Console.WriteLine($"DRY RUN: {input.Records.Count(value => value.Include)} selected records would be applied to {arguments.DestinationEnvironment}. Add --apply --confirm {arguments.DestinationEnvironment} to continue.");
    return;
}
if (!arguments.Confirm.Equals(arguments.DestinationEnvironment, StringComparison.Ordinal))
    throw new InvalidOperationException("--confirm must exactly match the destination environment.");
if (arguments.BootstrapUserId == Guid.Empty) throw new InvalidOperationException("--bootstrap-user-id is required for apply.");

await using var destination = CreateContext(arguments.DestinationConnection);
var report = await PromotionEngine.ImportAsync(destination, input, arguments,
    CreateStorage(arguments, source: true), CreateStorage(arguments, source: false));
Console.WriteLine(JsonSerializer.Serialize(report, PromotionJson.Options));

static El1teDbContext CreateContext(string connection) => new(new DbContextOptionsBuilder<El1teDbContext>()
    .UseSqlServer(connection).EnableSensitiveDataLogging(false).Options);
static IMediaStorage CreateStorage(PromotionArguments arguments, bool source)
{
    var root = source ? arguments.SourceMediaRoot : arguments.DestinationMediaRoot;
    var serviceUri = source ? arguments.SourceBlobServiceUri : arguments.DestinationBlobServiceUri;
    if (!string.IsNullOrWhiteSpace(root)) return new LocalMediaStorage(new MediaStorageOptions { LocalRoot = root });
    if (!string.IsNullOrWhiteSpace(serviceUri)) return new AzureBlobMediaStorage(new MediaStorageOptions { BlobServiceUri = serviceUri, ContainerName = arguments.MediaContainer });
    throw new InvalidOperationException(source
        ? "Source media requires --source-media-root or --source-blob-service-uri."
        : "Destination media requires --destination-media-root or --destination-blob-service-uri.");
}

internal sealed record PromotionManifest(
    int Version, string SourceEnvironment, string DestinationEnvironment, string SourceApiBase,
    string DestinationApiBase, DateTimeOffset CreatedAtUtc, IReadOnlyList<string> ProhibitedTypes,
    List<PromotionRecord> Records, string ManifestSha256 = "");

internal sealed record PromotionRecord(string Type, Guid Id, string Status, bool Include,
    string[] Dependencies, JsonElement Data, string? BlobSha256 = null);

internal sealed record PromotionReport(int Inserted, int Updated, int Skipped, int Selected,
    string ManifestSha256, IReadOnlyDictionary<string, int> Counts);

internal static class PromotionJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() }
    };
}

internal sealed record PromotionArguments(
    string Command, string SourceEnvironment, string DestinationEnvironment, string SourceConnection,
    string DestinationConnection, string ManifestPath, string SourceApiBase, string DestinationApiBase,
    bool Apply, string Confirm, Guid BootstrapUserId, string SourceMediaRoot, string DestinationMediaRoot,
    string SourceBlobServiceUri, string DestinationBlobServiceUri, string MediaContainer)
{
    public static PromotionArguments Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is not ("export" or "import")) Usage();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index++)
        {
            if (args[index] == "--apply") { values["apply"] = "true"; continue; }
            if (!args[index].StartsWith("--") || index + 1 >= args.Length) Usage();
            values[args[index][2..]] = args[++index];
        }
        string Required(string key, string? environment = null) => values.GetValueOrDefault(key)
            ?? (environment is null ? null : Environment.GetEnvironmentVariable(environment))
            ?? throw new InvalidOperationException($"--{key} is required.");
        return new PromotionArguments(args[0], Required("source-environment"), Required("destination-environment"),
            args[0] == "export" ? Required("source-connection", "PROMOTION_SOURCE_CONNECTION") : values.GetValueOrDefault("source-connection", "unused"),
            args[0] == "import" ? Required("destination-connection", "PROMOTION_DESTINATION_CONNECTION") : values.GetValueOrDefault("destination-connection", "unused"),
            Path.GetFullPath(Required("manifest")), Required("source-api-base"), Required("destination-api-base"),
            values.ContainsKey("apply"), values.GetValueOrDefault("confirm", ""),
            Guid.TryParse(values.GetValueOrDefault("bootstrap-user-id"), out var id) ? id : Guid.Empty,
            values.GetValueOrDefault("source-media-root", ""), values.GetValueOrDefault("destination-media-root", ""),
            values.GetValueOrDefault("source-blob-service-uri", ""), values.GetValueOrDefault("destination-blob-service-uri", ""),
            values.GetValueOrDefault("media-container", "media"));
    }
    private static void Usage() => throw new InvalidOperationException("Use export or import with explicit --source-environment, --destination-environment, --manifest, --source-api-base, and --destination-api-base. Database connections may be supplied through PROMOTION_SOURCE_CONNECTION and PROMOTION_DESTINATION_CONNECTION.");
}

internal static class PromotionEngine
{
    internal static readonly string[] ProhibitedTypes = [
        "User", "AdminInvitation", "AdminActivityLog", "ContactSubmission", "FeedbackSubmission", "Athlete",
        "AthleteDocument", "ConsentRecord", "Order", "OrderItem", "CommerceRefund", "SquareWebhookEvent",
        "CommerceOutboxMessage", "SquareCatalogImportRun", "AdminPasswordReset", "AdminMfaChallenge",
        "AuthenticationAttempt", "Telemetry"
    ];

    public static async Task<PromotionManifest> ExportAsync(El1teDbContext db, PromotionArguments args, IMediaStorage sourceStorage)
    {
        var records = new List<PromotionRecord>();
        await Add(db.SiteSettings, _ => true, _ => "Active", value => MediaDependenciesFromUrl(value.LogoUrl));
        await Add(db.ContentBlocks, value => value.IsPublished, value => value.IsPublished ? "Published" : "Draft", value => MediaDependenciesFromUrl(value.ImageUrl));
        await Add(db.Announcements, value => value.IsPublished, value => value.IsPublished ? "Published" : "Draft", value => MediaDependenciesFromUrl(value.ImageUrl));
        await Add(db.Events, value => value.IsPublished, value => value.IsPublished ? "Published" : "Draft", value => MediaDependenciesFromUrl(value.ImageUrl));
        await Add(db.Coaches, value => value.IsActive, value => value.IsActive ? "Active" : "Inactive", value => MediaDependenciesFromUrl(value.ImageUrl));
        await Add(db.HallOfFameInductees, value => value.IsActive, value => value.IsActive ? "Active" : "Inactive", value => MediaDependenciesFromUrl(value.PhotoUrl));
        await Add(db.Sponsors, value => value.IsActive, value => value.IsActive ? "Active" : "Inactive", value => MediaDependenciesFromUrl(value.LogoUrl));
        await Add(db.Faqs, value => value.IsActive, value => value.IsActive ? "Active" : "Inactive");
        await Add(db.GalleryAlbums, value => value.IsPublished, value => value.IsPublished ? "Published" : "Draft", value => MediaDependencies(value.CoverMediaAssetId));

        var includedAlbums = records.Where(value => value.Type == nameof(GalleryAlbum) && value.Include).Select(value => value.Id).ToHashSet();
        await Add(db.GalleryAlbumMedia, value => includedAlbums.Contains(value.GalleryAlbumId), _ => "Referenced", value => [value.GalleryAlbumId.ToString(), value.MediaAssetId.ToString()]);

        await Add(db.AllAmericanYears, value => value.IsPublished, value => value.IsPublished ? "Published" : "Draft", value => MediaDependencies(value.HeroMediaAssetId));
        var includedAllAmericanYears = records.Where(value => value.Type == nameof(AllAmericanYear) && value.Include).Select(value => value.Id).ToHashSet();
        await Add(db.AllAmericanYearMedia, value => includedAllAmericanYears.Contains(value.AllAmericanYearId), _ => "Referenced", value => [value.AllAmericanYearId.ToString(), value.MediaAssetId.ToString()]);
        await Add(db.AllAmericanRecipients, value => includedAllAmericanYears.Contains(value.AllAmericanYearId), value => value.IsActive ? "Active" : "Inactive", value => [value.AllAmericanYearId.ToString(), .. MediaDependencies(value.PhotoMediaAssetId)]);
        await Add(db.AllAmericanPerformances, value => includedAllAmericanYears.Contains(value.AllAmericanYearId), value => value.IsActive ? "Active" : "Inactive", value => [value.AllAmericanYearId.ToString()]);
        var includedPerformances = records.Where(value => value.Type == nameof(AllAmericanPerformance) && value.Include).Select(value => value.Id).ToHashSet();
        await Add(db.AllAmericanPerformanceRecipients, value => includedPerformances.Contains(value.AllAmericanPerformanceId), _ => "Referenced", value => [value.AllAmericanPerformanceId.ToString(), value.AllAmericanRecipientId.ToString()]);

        await Add(db.ProductCategories, value => value.IsActive, value => value.IsActive ? "Active" : "Inactive");
        await Add(db.Products, value => value.Status == StoreProductStatus.Published, value => value.Status.ToString(), value => value.CategoryId.HasValue ? [value.CategoryId.Value.ToString()] : []);
        var includedProducts = records.Where(value => value.Type == nameof(Product) && value.Include).Select(value => value.Id).ToHashSet();
        await Add(db.ProductMedia, value => includedProducts.Contains(value.ProductId), _ => "Referenced", value => [value.ProductId.ToString(), value.MediaAssetId.ToString()]);
        await Add(db.ProductOptions, value => includedProducts.Contains(value.ProductId), value => value.IsActive ? "Active" : "Inactive", value => [value.ProductId.ToString()]);
        var includedOptions = records.Where(value => value.Type == nameof(ProductOption) && value.Include).Select(value => value.Id).ToHashSet();
        await Add(db.ProductOptionValues, value => includedOptions.Contains(value.ProductOptionId), value => value.IsActive ? "Active" : "Inactive", value => [value.ProductOptionId.ToString(), .. MediaDependencies(value.SwatchMediaAssetId)]);
        await Add(db.ProductVariants, value => includedProducts.Contains(value.ProductId), value => value.IsActive ? "Active" : "Inactive", value => [value.ProductId.ToString()]);
        var includedVariants = records.Where(value => value.Type == nameof(ProductVariant) && value.Include).Select(value => value.Id).ToHashSet();
        await Add(db.ProductVariantOptionValues, value => includedVariants.Contains(value.ProductVariantId), _ => "Referenced", value => [value.ProductVariantId.ToString(), value.ProductOptionValueId.ToString()], value => value.ProductVariantId);
        await Add(db.ProductModifierGroups, value => includedProducts.Contains(value.ProductId), value => value.IsActive ? "Active" : "Inactive", value => [value.ProductId.ToString()]);
        var includedGroups = records.Where(value => value.Type == nameof(ProductModifierGroup) && value.Include).Select(value => value.Id).ToHashSet();
        await Add(db.ProductModifierValues, value => includedGroups.Contains(value.ProductModifierGroupId), value => value.IsActive ? "Active" : "Inactive", value => [value.ProductModifierGroupId.ToString(), .. MediaDependencies(value.OverlayMediaAssetId)]);
        await Add(db.ProductVisualizerLayers, value => includedProducts.Contains(value.ProductId), _ => "Referenced", value => [value.ProductId.ToString(), value.MediaAssetId.ToString()]);

        var referencedMedia = records.Where(value => value.Include).SelectMany(value => value.Dependencies)
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty).Where(value => value != Guid.Empty).ToHashSet();
        await Add(db.MediaAssets, value => referencedMedia.Contains(value.Id), value => value.IsActive ? "Active" : "Inactive");
        var includedMedia = records.Where(value => value.Type == nameof(MediaAsset) && value.Include).Select(value => value.Id).ToHashSet();
        await Add(db.MediaDerivatives, value => includedMedia.Contains(value.MediaAssetId), _ => "Referenced", value => [value.MediaAssetId.ToString()]);

        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            var storageKey = record.Type switch
            {
                nameof(MediaAsset) => record.Data.Deserialize<MediaAsset>(PromotionJson.Options)?.StorageKey,
                nameof(MediaDerivative) => record.Data.Deserialize<MediaDerivative>(PromotionJson.Options)?.StorageKey,
                _ => null
            };
            if (storageKey is null) continue;
            await using var stream = await sourceStorage.OpenReadAsync(storageKey, default)
                ?? throw new InvalidOperationException($"Media object is missing: {storageKey}");
            records[index] = record with { BlobSha256 = await HashAsync(stream) };
        }

        var unsigned = new PromotionManifest(1, args.SourceEnvironment, args.DestinationEnvironment,
            args.SourceApiBase.TrimEnd('/'), args.DestinationApiBase.TrimEnd('/'), DateTimeOffset.UtcNow,
            ProhibitedTypes, records);
        return unsigned with { ManifestSha256 = ManifestHash(unsigned) };

        async Task Add<TEntity>(IQueryable<TEntity> query, Func<TEntity, bool> include, Func<TEntity, string> status,
            Func<TEntity, string[]>? dependencies = null, Func<TEntity, Guid>? id = null) where TEntity : class
        {
            foreach (var item in await query.AsNoTracking().ToListAsync())
            {
                var identifier = id?.Invoke(item) ?? EntityId(item);
                records.Add(new PromotionRecord(typeof(TEntity).Name, identifier, status(item), include(item),
                    dependencies?.Invoke(item) ?? [], JsonSerializer.SerializeToElement(item, PromotionJson.Options)));
            }
        }
    }

    public static void Validate(PromotionManifest manifest, PromotionArguments args)
    {
        if (manifest.Version != 1 || manifest.SourceEnvironment != args.SourceEnvironment || manifest.DestinationEnvironment != args.DestinationEnvironment)
            throw new InvalidOperationException("Manifest environments or version do not match the command.");
        if (manifest.Records.Any(value => ProhibitedTypes.Contains(value.Type)))
            throw new InvalidOperationException("The manifest contains a prohibited record type.");
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(manifest.ManifestSha256), Convert.FromHexString(ManifestHash(manifest with { ManifestSha256 = "" }))))
            throw new InvalidOperationException("The manifest hash does not match its contents.");
        var recordsById = manifest.Records.GroupBy(value => value.Id).ToDictionary(value => value.Key, value => value.ToList());
        var missingDependencies = manifest.Records.Where(value => value.Include)
            .SelectMany(value => value.Dependencies.Select(dependency => (Record: value, Dependency: dependency)))
            .Where(value => Guid.TryParse(value.Dependency, out var id) &&
                recordsById.TryGetValue(id, out var dependencyRecords) && dependencyRecords.All(record => !record.Include))
            .Select(value => $"{value.Record.Type}:{value.Record.Id} -> {value.Dependency}").ToList();
        if (missingDependencies.Count > 0)
            throw new InvalidOperationException($"Selected records have excluded dependencies: {string.Join(", ", missingDependencies)}");
    }

    public static async Task<PromotionReport> ImportAsync(El1teDbContext db, PromotionManifest manifest, PromotionArguments args,
        IMediaStorage sourceStorage, IMediaStorage destinationStorage)
    {
        var inserted = 0; var updated = 0; var skipped = 0;
        var counts = new Dictionary<string, int>();
        foreach (var record in manifest.Records.Where(value => value.Include && value.BlobSha256 is not null))
        {
            var storageKey = record.Type switch
            {
                nameof(MediaAsset) => record.Data.Deserialize<MediaAsset>(PromotionJson.Options)?.StorageKey,
                nameof(MediaDerivative) => record.Data.Deserialize<MediaDerivative>(PromotionJson.Options)?.StorageKey,
                _ => null
            };
            if (storageKey is null) continue;
            await using var source = await sourceStorage.OpenReadAsync(storageKey, default)
                ?? throw new InvalidOperationException($"Selected source Blob is missing: {storageKey}");
            await using var buffer = new MemoryStream(); await source.CopyToAsync(buffer); buffer.Position = 0;
            if (!string.Equals(await HashAsync(buffer), record.BlobSha256, StringComparison.Ordinal))
                throw new InvalidOperationException($"Source Blob hash mismatch: {storageKey}");
            buffer.Position = 0;
            await destinationStorage.SaveAsAsync(buffer, storageKey, default);
            await using var verified = await destinationStorage.OpenReadAsync(storageKey, default)
                ?? throw new InvalidOperationException($"Destination Blob is missing after copy: {storageKey}");
            if (!string.Equals(await HashAsync(verified), record.BlobSha256, StringComparison.Ordinal))
                throw new InvalidOperationException($"Destination Blob hash mismatch: {storageKey}");
        }

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        foreach (var record in manifest.Records.Where(value => value.Include).OrderBy(value => ImportOrder(value.Type)))
        {
            var outcome = await Upsert(record);
            if (outcome == 1) inserted++; else if (outcome == 2) updated++; else skipped++;
            counts[record.Type] = counts.GetValueOrDefault(record.Type) + 1;
        }
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return new PromotionReport(inserted, updated, skipped, inserted + updated + skipped, manifest.ManifestSha256, counts);

        async Task<int> Upsert(PromotionRecord record) => record.Type switch
        {
            nameof(SiteSetting) => await UpsertEntity<SiteSetting>(record),
            nameof(ContentBlock) => await UpsertEntity<ContentBlock>(record),
            nameof(Announcement) => await UpsertEntity<Announcement>(record),
            nameof(Event) => await UpsertEntity<Event>(record),
            nameof(Coach) => await UpsertEntity<Coach>(record),
            nameof(HallOfFameInductee) => await UpsertEntity<HallOfFameInductee>(record),
            nameof(Sponsor) => await UpsertEntity<Sponsor>(record),
            nameof(Faq) => await UpsertEntity<Faq>(record),
            nameof(MediaAsset) => await UpsertMedia(record),
            nameof(MediaDerivative) => await UpsertEntity<MediaDerivative>(record),
            nameof(GalleryAlbum) => await UpsertEntity<GalleryAlbum>(record),
            nameof(GalleryAlbumMedia) => await UpsertEntity<GalleryAlbumMedia>(record),
            nameof(AllAmericanYear) => await UpsertEntity<AllAmericanYear>(record),
            nameof(AllAmericanYearMedia) => await UpsertEntity<AllAmericanYearMedia>(record),
            nameof(AllAmericanRecipient) => await UpsertEntity<AllAmericanRecipient>(record),
            nameof(AllAmericanPerformance) => await UpsertEntity<AllAmericanPerformance>(record),
            nameof(AllAmericanPerformanceRecipient) => await UpsertEntity<AllAmericanPerformanceRecipient>(record),
            nameof(ProductCategory) => await UpsertEntity<ProductCategory>(record),
            nameof(Product) => await UpsertEntity<Product>(record),
            nameof(ProductMedia) => await UpsertEntity<ProductMedia>(record),
            nameof(ProductOption) => await UpsertEntity<ProductOption>(record),
            nameof(ProductOptionValue) => await UpsertEntity<ProductOptionValue>(record),
            nameof(ProductVariant) => await UpsertVariant(record),
            nameof(ProductModifierGroup) => await UpsertEntity<ProductModifierGroup>(record),
            nameof(ProductModifierValue) => await UpsertEntity<ProductModifierValue>(record),
            nameof(ProductVisualizerLayer) => await UpsertEntity<ProductVisualizerLayer>(record),
            nameof(ProductVariantOptionValue) => await UpsertVariantValue(record),
            _ => throw new InvalidOperationException($"Unsupported promotion record type: {record.Type}")
        };

        async Task<int> UpsertEntity<TEntity>(PromotionRecord record) where TEntity : class
        {
            var value = record.Data.Deserialize<TEntity>(PromotionJson.Options)!;
            RewriteUrls(value, manifest.SourceApiBase, manifest.DestinationApiBase);
            var existing = await db.Set<TEntity>().FindAsync(EntityId(value));
            if (existing is null) { db.Add(value); return 1; }
            db.Entry(existing).CurrentValues.SetValues(value); return 2;
        }
        async Task<int> UpsertMedia(PromotionRecord record)
        {
            var value = record.Data.Deserialize<MediaAsset>(PromotionJson.Options)!;
            value.UploadedByUserId = args.BootstrapUserId;
            value.PublicUrl = $"{manifest.DestinationApiBase}/media/{value.Id}";
            return await UpsertValue(value);
        }
        async Task<int> UpsertVariant(PromotionRecord record)
        {
            var value = record.Data.Deserialize<ProductVariant>(PromotionJson.Options)!;
            value.OnHandQuantity = 0; value.ReservedQuantity = 0; value.RowVersion = [];
            return await UpsertValue(value);
        }
        async Task<int> UpsertValue<TEntity>(TEntity value) where TEntity : class
        {
            var existing = await db.Set<TEntity>().FindAsync(EntityId(value));
            if (existing is null) { db.Add(value); return 1; }
            db.Entry(existing).CurrentValues.SetValues(value); return 2;
        }
        async Task<int> UpsertVariantValue(PromotionRecord record)
        {
            var value = record.Data.Deserialize<ProductVariantOptionValue>(PromotionJson.Options)!;
            var existing = await db.ProductVariantOptionValues.FindAsync(value.ProductVariantId, value.ProductOptionValueId);
            if (existing is not null) return 0;
            db.Add(value); return 1;
        }
    }

    private static int ImportOrder(string type) => type switch
    {
        nameof(SiteSetting) or nameof(ContentBlock) or nameof(Announcement) or nameof(Event) or nameof(Coach) or nameof(HallOfFameInductee) or nameof(Sponsor) or nameof(Faq) => 10,
        nameof(MediaAsset) => 20, nameof(MediaDerivative) => 21, nameof(GalleryAlbum) or nameof(AllAmericanYear) => 30,
        nameof(GalleryAlbumMedia) or nameof(AllAmericanYearMedia) or nameof(AllAmericanRecipient) => 31,
        nameof(AllAmericanPerformance) => 32, nameof(AllAmericanPerformanceRecipient) => 33,
        nameof(ProductCategory) => 40, nameof(Product) => 41,
        nameof(ProductMedia) or nameof(ProductOption) or nameof(ProductVariant) or nameof(ProductModifierGroup) => 42,
        nameof(ProductOptionValue) or nameof(ProductModifierValue) => 43,
        nameof(ProductVariantOptionValue) or nameof(ProductVisualizerLayer) => 44, _ => 100
    };
    private static Guid EntityId(object value) => value switch
    {
        EntityBase entity => entity.Id,
        CmsEntityBase entity => entity.Id,
        _ => throw new InvalidOperationException($"{value.GetType().Name} does not expose a supported ID.")
    };
    private static string[] MediaDependencies(Guid? id) => id.HasValue ? [id.Value.ToString()] : [];
    private static string[] MediaDependenciesFromUrl(string? url) => TryMediaId(url, out var id) ? [id.ToString()] : [];
    private static bool TryMediaId(string? url, out Guid id)
    {
        id = Guid.Empty; if (string.IsNullOrWhiteSpace(url)) return false;
        var marker = "/media/"; var start = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return start >= 0 && Guid.TryParse(url.AsSpan(start + marker.Length, Math.Min(36, url.Length - start - marker.Length)), out id);
    }
    private static void RewriteUrls(object value, string source, string destination)
    {
        foreach (var property in value.GetType().GetProperties().Where(value => value.CanRead && value.CanWrite && value.PropertyType == typeof(string)))
            if (property.GetValue(value) is string text && text.StartsWith(source, StringComparison.OrdinalIgnoreCase)) property.SetValue(value, destination + text[source.Length..]);
    }
    private static string ManifestHash(PromotionManifest manifest)
    {
        var normalizedManifest = manifest with
        {
            ManifestSha256 = "",
            Records = manifest.Records.Select(value => value with { Include = false }).ToList()
        };
        var normalized = JsonSerializer.Serialize(normalizedManifest, PromotionJson.Options);
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized)));
    }
    private static async Task<string> HashAsync(Stream stream)
    {
        if (stream.CanSeek) stream.Position = 0;
        using var hash = SHA256.Create();
        return Convert.ToHexString(await hash.ComputeHashAsync(stream));
    }
}
