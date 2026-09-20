using System.Runtime.CompilerServices;
using Hangfire;
using Hangfire.Common;
using Hangfire.Logging;

namespace Sitecraft.Api.Tests;

/// <summary>
/// Hangfire'in statik filtre tablosu (<c>GlobalJobFilters</c> → <c>JobFilterProviders</c>) surec
/// basina yalniz bir kez kurulur ve kurulurken o anki log saglayicisini kullanir. Her
/// <c>AddHangfire</c> cagrisi saglayiciyi kendi fabrikasinin LoggerFactory'sine cevirir; kurulum
/// ilk kez sokulmus bir fabrikadan sonra tetiklenirse cctor ObjectDisposedException ile patlar.
/// Statik kurucu bir kez patlayinca surec boyunca patlak kalir, yani sonraki her is kuyruga alma
/// TypeInitializationException verip 500 doner.
///
/// Hangi fabrikanin once sokuldugu test sinifi sirasina bagli oldugu icin belirti kaygandir:
/// sinif tek basina kosunca gecer, tam kosuda onlarca test birden doker.
/// (<see cref="PostgresFixture.InitializeAsync"/> migration icin bir fabrika kurup hemen sokuyor —
/// bu adayi uretiyor.)
///
/// Cozum: kurulumu, hicbir fabrikanin omrune bagli olmayan bir saglayiciyla, ilk fabrika
/// kurulmadan once zorlamak. Sonraki <c>AddHangfire</c> cagrilari saglayiciyi kendilerine cevirse
/// de statik tablo coktan kurulmus olur.
/// </summary>
internal static class HangfireStaticInit
{
    [ModuleInitializer]
    internal static void Init()
    {
        LogProvider.SetCurrentLogProvider(NoOpLogProvider.Instance);

        // Statik kuruculari simdi, saglayici canliyken calistir.
        _ = GlobalJobFilters.Filters;
        _ = JobFilterProviders.Providers;
    }

    private sealed class NoOpLogProvider : ILogProvider
    {
        public static readonly NoOpLogProvider Instance = new();

        public ILog GetLogger(string name) => NoOpLog.Instance;

        private sealed class NoOpLog : ILog
        {
            public static readonly NoOpLog Instance = new();

            public bool Log(LogLevel logLevel, Func<string>? messageFunc, Exception? exception = null) => false;
        }
    }
}
