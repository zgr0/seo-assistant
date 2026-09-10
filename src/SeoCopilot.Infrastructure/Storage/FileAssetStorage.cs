using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Infrastructure.Storage;

public sealed class AssetStorageOptions
{
    public const string Section = "Assets";

    /// <summary>Uretilen gorsellerin kok dizini; goreli verilirse calisma dizinine gore cozulur.</summary>
    public string Directory { get; set; } = "App_Data/assets";
}

/// <summary>
/// Yerel diskte gorsel deposu. Anahtar dizin ayirici icerebilir; kok disina cikan
/// anahtarlar reddedilir.
/// </summary>
public sealed class FileAssetStorage(IOptions<AssetStorageOptions> options) : IAssetStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.Directory);

    public async Task<string> SaveAsync(string key, byte[] content, CancellationToken ct = default)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, ct);
        return key;
    }

    public async Task<byte[]?> ReadAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
    }

    private string Resolve(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root, StringComparison.Ordinal))
            throw new InvalidOperationException("Geçersiz depolama anahtarı");

        return path;
    }
}
