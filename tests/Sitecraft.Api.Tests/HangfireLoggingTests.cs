using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Sitecraft.Api.Tests;

/// <summary>
/// Hangfire'in log saglayicisi surec genelinde statiktir (<c>GlobalConfiguration</c>): her
/// <c>AddHangfire</c> cagrisi saglayiciyi kendi host'unun <c>ILoggerFactory</c>'sine cevirir.
/// Bir fabrika sokulup digeri yasamaya devam ederse, yasayan fabrikadaki ilk Hangfire istemcisi
/// cozumu sokulmus LoggerFactory'ye gider ve ObjectDisposedException atar.
///
/// Testler paralel kostugu icin belirti kaygandi: CI'da rastgele bir tarama testi doker.
/// </summary>
public class HangfireLoggingTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public void Job_client_resolves_after_another_factory_is_disposed()
    {
        using var alive = fixture.CreateFactory();
        alive.CreateClient();

        // Ikinci fabrika statik saglayiciyi kendine cevirir, sonra sokulur.
        var doomed = fixture.CreateFactory();
        doomed.CreateClient();
        doomed.Dispose();

        // Hangfire istemcisi ilk kez burada cozulur — sokulmus fabrikanin logger'ina gitmemeli.
        Assert.NotNull(alive.Services.GetRequiredService<IBackgroundJobClient>());
    }
}
