namespace SeoCopilot.Application.Abstractions;

/// <summary>Metinden gorsel ureten servis (FLUX). Anahtar yoksa kapali kalir.</summary>
public interface IImageGenerator
{
    /// <summary>Anahtar tanimli degilse false — cagiran taraf gorseli sessizce atlar.</summary>
    bool IsEnabled { get; }

    /// <summary>Basarisiz uretimde null doner; istisna firlatmaz (gorsel zorunlu degil).</summary>
    Task<GeneratedImage?> GenerateAsync(
        string prompt, string aspectRatio, CancellationToken ct = default);
}

public record GeneratedImage(byte[] Content, string ContentType, int Width, int Height, string Model);
