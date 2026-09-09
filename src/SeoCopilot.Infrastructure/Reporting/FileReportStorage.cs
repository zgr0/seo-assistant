using Microsoft.Extensions.Options;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Infrastructure.Reporting;

public sealed class ReportStorageOptions
{
    public const string Section = "Reports";

    /// <summary>Rapor dosyalarinin kok dizini; goreli verilirse calisma dizinine gore cozulur.</summary>
    public string Directory { get; set; } = "App_Data/reports";
}

/// <summary>
/// Yerel diskte dosya deposu. Anahtar dizin ayirici icerebilir; kok disina cikan
/// anahtarlar reddedilir.
/// </summary>
public sealed class FileReportStorage(IOptions<ReportStorageOptions> options) : IReportStorage
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
