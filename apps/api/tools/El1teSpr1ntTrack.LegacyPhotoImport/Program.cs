using Azure.Core;
using Azure.Identity;
using El1teSpr1ntTrack.Infrastructure.Data;
using El1teSpr1ntTrack.Infrastructure.Media;
using El1teSpr1ntTrack.LegacyPhotoImport;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

const string demo = "el1tesprint-demo-neauu2";
using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };
try
{
    var settings = ImportArguments.Parse(args, demo);
    var manifest = await PhotoManifest.LoadAsync(settings.Manifest, settings.ManifestHash, cancel.Token);
    var groups = settings.All ? manifest.Groups : manifest.SelectPilot();
    Console.WriteLine($"mode={(settings.Apply ? "apply-demo" : "dry-run")} assets={groups.Count} sources={groups.Sum(g => g.Sources.Count)} " +
        $"original-bytes={groups.Sum(g => g.Sources[0].ByteLength)} max-preview-bytes={(long)groups.Count * LegacyPhotoImages.MaxPreviewBytes}");
    var result = new ImportResult();
    if (!settings.Apply)
    {
        await PhotoImport.RunAsync(manifest, groups, result, null, cancel.Token);
    }
    else
    {
        var azure = new AzureCliCredential();
        var sql = new SqlConnection($"Server=tcp:{demo}-sql.database.windows.net,1433;Initial Catalog=el1tesprint-demo-db;Encrypt=True;TrustServerCertificate=False;Connect Timeout=15");
        sql.AccessTokenCallback = async (_, token) =>
        {
            var access = await azure.GetTokenAsync(new TokenRequestContext(["https://database.windows.net/.default"]), token);
            return new SqlAuthenticationToken(access.Token, access.ExpiresOn);
        };
        var dbOptions = new DbContextOptionsBuilder<El1teDbContext>().UseSqlServer(sql, contextOwnsConnection: true).Options;
        await using var db = new El1teDbContext(dbOptions);
        var blobs = new LegacyPhotoBlobs(new Uri("https://el1tesprintdemoneauu2med.blob.core.windows.net"), azure);
        if (!await db.Database.CanConnectAsync(cancel.Token))
            throw new InvalidDataException("demo-sql-unavailable");
        await PhotoImport.RunAsync(manifest, groups, result, new PhotoImport(db, blobs), cancel.Token);
    }
    Console.WriteLine($"verified={result.Verified} created={result.Created} resumed={result.Resumed} " +
        $"sources={result.Sources} page-links={result.PageLinks} duplicate-sources={result.Sources - result.Verified} " +
        $"failed={result.Failed} preview-bytes={result.PreviewBytes}");
    return result.Failed == 0 ? 0 : 1;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Canceled. Rerun the same command to resume.");
    return 130;
}
catch (InvalidDataException error)
{
    Console.Error.WriteLine(error.Message);
    return 2;
}
catch (Exception error)
{
    Console.Error.WriteLine($"Import could not start ({error.GetType().Name}). Check demo access and configuration.");
    return 2;
}

public sealed record ImportArguments(string Manifest, string ManifestHash, bool Apply, bool All)
{
    public static ImportArguments Parse(string[] args, string demo)
    {
        string? manifest = null, hash = null, target = null;
        bool apply = false, all = false, launchComplete = false;
        var seen = new HashSet<string>();
        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            if (!seen.Add(option)) throw Usage();
            switch (option)
            {
                case "--manifest" when index + 1 < args.Length: manifest = args[++index]; break;
                case "--manifest-sha256" when index + 1 < args.Length: hash = args[++index].ToLowerInvariant(); break;
                case "--confirm-demo" when index + 1 < args.Length: target = args[++index]; break;
                case "--apply": apply = true; break;
                case "--all": all = true; break;
                case "--launch-complete": launchComplete = true; break;
                default: throw Usage();
            }
        }
        if (string.IsNullOrWhiteSpace(manifest) || hash?.Length != 64 || !hash.All(char.IsAsciiHexDigitLower) ||
            (apply ? target != demo : target is not null || launchComplete) || (apply && all && !launchComplete) ||
            (launchComplete && !all))
            throw Usage();
        return new ImportArguments(manifest, hash, apply, all);
    }

    private static InvalidDataException Usage() => new(
        "Use --manifest PATH --manifest-sha256 HASH [--all]. To write, also use --apply --confirm-demo el1tesprint-demo-neauu2. Full writes additionally require --launch-complete after sign-off.");
}
