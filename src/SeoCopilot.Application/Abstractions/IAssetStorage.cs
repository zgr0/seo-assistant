namespace SeoCopilot.Application.Abstractions;

/// <summary>Uretilen gorsellerin bayt deposu — rapor deposuyla ayni sozlesme, ayri kok.</summary>
public interface IAssetStorage
{
    Task<string> SaveAsync(string key, byte[] content, CancellationToken ct = default);

    Task<byte[]?> ReadAsync(string key, CancellationToken ct = default);
}
