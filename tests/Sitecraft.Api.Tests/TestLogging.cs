using Microsoft.Extensions.Logging;

namespace Sitecraft.Api.Tests;

/// <summary>
/// Butun test fabrikalarinin paylastigi, hicbir host ile sokulmeyen ILoggerFactory.
///
/// Hangfire'in log saglayicisi surec genelinde statiktir: her <c>AddHangfire</c> cagrisi
/// <c>GlobalConfiguration</c>'i o host'un ILoggerFactory'sine baglar. Fabrikalar paralel
/// kosan test siniflarinda gelip gittigi icin saglayici sik sik baska bir fabrikanin
/// (cogu zaman coktan sokulmus) logger'ini gosterir ve ilk Hangfire istemcisi cozumu
/// ObjectDisposedException atar.
///
/// Cozum saglayiciyi kovalamak degil, gosterdigi fabrikayi olumsuz kilmak: tek bir
/// factory, <c>Dispose</c> bos. Test kosusu bitince surecle birlikte gider.
/// Bkz. <see cref="HangfireLoggingTests"/>.
/// </summary>
internal static class TestLogging
{
    private static readonly ILoggerFactory Inner = LoggerFactory.Create(builder => builder
        .AddSimpleConsole()
        // Bilgi seviyesi her istegi ve her SQL'i doker; hata ayiklarken gecici olarak dusurun.
        .SetMinimumLevel(LogLevel.Warning));

    public static ILoggerFactory Shared { get; } = new NonDisposableLoggerFactory(Inner);

    private sealed class NonDisposableLoggerFactory(ILoggerFactory inner) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => inner.CreateLogger(categoryName);

        public void AddProvider(ILoggerProvider provider) => inner.AddProvider(provider);

        /// <summary>Host sokulurken paylasilan factory kapanmamali — butun mesele bu.</summary>
        public void Dispose() { }
    }
}
