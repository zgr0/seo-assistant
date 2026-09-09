using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Entities.Performance;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Entities.Sites;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>
/// Cok sayfali tarama motoru. Kesintisiz BFS: FIFO bir kuyruk (<see cref="Channel"/>) ile
/// <see cref="CrawlSettings.Concurrency"/> adet getirici surekli calisir — seviye sinirinda
/// beklenmez, yani yavas bir sayfa digerlerini bosta bekletmez.
///
/// Nezaket <see cref="RequestPacer"/> ile saglanir; crawl'in tum giden istekleri (sayfa,
/// varlik yoklamasi, gorsel olcumu) tek butceyi paylasir.
///
/// Getirmeler paralel; entity uretimi, kural calistirma, kuyruga ekleme ve veritabanina
/// yazma hep tek is parcaciginda (DbContext thread-safe degil, paylasilan kumeler kilitsiz).
/// </summary>
public sealed class CrawlEngine(
    ISiteRepository repository,
    IPageExtractor extractor,
    IRobotsSource robotsSource,
    ISitemapSource sitemapSource,
    IPageSpeedClient pageSpeed,
    IRuleRunner ruleRunner,
    ILogger<CrawlEngine> logger)
{
    private const int SaveBatchSize = 50;
    private const int MaxUrlLength = 2048;
    private const int MaxAnchorLength = 512;

    /// <summary>BLOCKED_BY_ROBOTS_TXT bulgusunda saklanan azami ornek sayisi.</summary>
    private const int MaxBlockedTracked = 100;

    /// <summary>Link grafiginde yonlendirme zinciri izlenirken azami atlama.</summary>
    private const int MaxRedirectHops = 5;

    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Iptal istegi bu siklikta sorulur — her sayfada sormak gereksiz veritabani trafigi.</summary>
    private static readonly TimeSpan CancelCheckInterval = TimeSpan.FromSeconds(2);

    private sealed record FrontierItem(Uri Url, int Depth);

    /// <summary>Tohumlama ciktisi: baslangic kuyrugu ve sitemap gercekleri.</summary>
    private sealed record Seed(List<FrontierItem> Frontier, HashSet<string> SitemapUrls, bool SitemapFound);

    /// <summary>
    /// Crawl'i bastan sona yurutur; pages / page_links / issues satirlarini ve crawl ozet
    /// alanlarini yazar. Durumu doner — StartedAt/FinishedAt cagirana ait.
    /// </summary>
    /// <param name="cancelRequested">
    /// En sik <see cref="CancelCheckInterval"/> araligiyla sorulur; true donerse tarama o ana
    /// kadarki verilerle kapatilir ve <see cref="CrawlStatus.Cancelled"/> donulur.
    /// Kullanicinin iptal istegi buradan gelir.
    /// </param>
    public async Task<CrawlStatus> RunAsync(
        Crawl crawl, Site site, Func<CancellationToken, Task<bool>>? cancelRequested, CancellationToken ct = default)
    {
        var settings = site.CrawlSettings;
        var baseUri = ResolveBaseUri(site.BaseUrl);
        var maxPages = Math.Max(1, settings.MaxPages);
        var maxDepth = Math.Max(0, settings.MaxDepth);
        var concurrency = Math.Clamp(settings.Concurrency, 1, 16);
        var delayMs = Math.Max(0, settings.DelayMs);

        var include = Compile(settings.IncludePatterns);
        var exclude = Compile(settings.ExcludePatterns);
        var robots = await robotsSource.GetAsync(baseUri, ct);

        // Nezaket butcesi: her delayMs penceresinde concurrency istek. Tum giden istekler paylasir.
        using var pacer = new RequestPacer(concurrency, delayMs);

        // Gorsel olcumu gibi sayfa disi istekler de robots'a ve ayni nezaket butcesine uyar.
        var fetchOptions = new PageFetchOptions(baseUri, settings.RenderJs, robots, pacer);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var assetSeen = new HashSet<string>(StringComparer.Ordinal);
        var assets = new List<FrontierItem>();
        var blocked = new HashSet<string>(StringComparer.Ordinal);
        var pages = new List<Page>();
        var links = new List<PageLink>();
        var pageFindings = new List<(Page Page, RuleFinding Finding)>();
        var pageScores = new List<int>();
        var truncated = false;
        var cancelled = false;
        var saved = 0;

        var seed = await SeedFrontierAsync(
            baseUri, robots, include, exclude, seen, assetSeen, assets, blocked, maxPages, ct);
        logger.LogInformation(
            "Crawl {CrawlId} basliyor: {Seed} tohum URL, maxPages={MaxPages} maxDepth={MaxDepth} concurrency={Concurrency}",
            crawl.Id, seed.Frontier.Count, maxPages, maxDepth, concurrency);

        // Kuyruk FIFO oldugu ve cocuklar ebeveynin arkasina eklendigi icin sira yine BFS.
        var frontier = Channel.CreateUnbounded<FrontierItem>(
            new UnboundedChannelOptions { SingleWriter = true });
        var fetched = Channel.CreateUnbounded<(FrontierItem Item, ExtractedPage Page)>(
            new UnboundedChannelOptions { SingleReader = true });

        // Iptalde ucus halindeki getirmeleri de birakmak icin — dis token'a bagli.
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var workers = new Task[concurrency];
        for (var i = 0; i < concurrency; i++)
            workers[i] = FetchLoopAsync(frontier.Reader, fetched.Writer, fetchOptions, pacer, stop.Token);

        // Kuyruga yazan tek yer burasi (tuketici is parcacigi) — kilit gerekmez.
        var dispatched = 0;
        var pending = 0;
        var seedIds = new HashSet<Guid>();

        void Enqueue(Uri url, int depth)
        {
            // Tavana takilan URL kesfedilmis sayilir (pages_discovered) ama getirilmez.
            if (depth > maxDepth || dispatched >= maxPages)
            {
                truncated = true;
                return;
            }

            frontier.Writer.TryWrite(new FrontierItem(url, depth));
            dispatched++;
            pending++;
        }

        foreach (var item in seed.Frontier) Enqueue(item.Url, item.Depth);

        var lastCancelCheck = Stopwatch.GetTimestamp();

        try
        {
            // Tek is parcacigi: entity uretimi, kural calistirma, kuyruga ekleme, kayit.
            while (pending > 0)
            {
                var (item, extracted) = await fetched.Reader.ReadAsync(ct);
                pending--;

                var page = BuildPage(crawl.Id, item, extracted);
                pages.Add(page);
                if (item.Depth == 0) seedIds.Add(page.Id);

                // 2xx donen ama HTML olmayan icerik sayfa degil — sayfa kurallari uygulanmaz.
                if (extracted.StatusCode is < 200 or >= 300 || extracted.IsHtml)
                {
                    var outcome = ruleRunner.Run(extracted);
                    pageScores.Add(outcome.Score);
                    foreach (var finding in outcome.Findings)
                        pageFindings.Add((page, finding));
                }

                // Yonlendirilen URL'in kendi belgesi yok; govdedeki linkler hedefe ait.
                // Buraya yazilirsa page_links'te ayni link iki kez cikar ve inlink_count sisir.
                // Onun yerine hedef kuyruga alinir, linkler orada toplanir.
                if (extracted.RedirectTo is not null)
                {
                    // Derinlik artmaz — yonlendirme bir link atlamasi degil.
                    // Hedefe baska bir sayfa zaten link verdiyse `seen` ikinci kez almaz.
                    if (UrlNormalizer.TryNormalize(extracted.RedirectTo, out var target)
                        && TryAccept(target, baseUri, robots, include, exclude, seen, blocked))
                    {
                        Enqueue(target, item.Depth);
                    }
                }
                else
                {
                    var (internalCount, externalCount) = CollectLinks(crawl.Id, page, extracted, links);
                    page.OutlinkInternal = internalCount;
                    page.OutlinkExternal = externalCount;

                    foreach (var link in extracted.Links)
                    {
                        if (!link.IsInternal || link.IsNofollow) continue;

                        // Ikili varliklar kuyruga girmez; ayri listede yalniz durumu yoklanir.
                        if (UrlNormalizer.IsLikelyAsset(link.Url))
                        {
                            if (TryAccept(link.Url, baseUri, robots, include, exclude, assetSeen, blocked))
                                assets.Add(new FrontierItem(link.Url, item.Depth + 1));
                            continue;
                        }

                        if (!TryAccept(link.Url, baseUri, robots, include, exclude, seen, blocked)) continue;
                        Enqueue(link.Url, item.Depth + 1);
                    }
                }

                if (pages.Count - saved >= SaveBatchSize)
                {
                    saved = await PersistPagesAsync(pages, saved, crawl, pages.Count, ct);
                    logger.LogInformation(
                        "Crawl {CrawlId}: {Crawled} sayfa islendi, kuyrukta {Pending}",
                        crawl.Id, pages.Count, pending);
                }

                // Iptal sorgusu veritabanina gider — sayfa basina degil, zamana bagli sorulur.
                if (cancelRequested is not null
                    && Stopwatch.GetElapsedTime(lastCancelCheck) >= CancelCheckInterval)
                {
                    lastCancelCheck = Stopwatch.GetTimestamp();
                    if (await cancelRequested(ct))
                    {
                        logger.LogInformation("Crawl {CrawlId} kullanici tarafindan iptal edildi", crawl.Id);
                        cancelled = true;
                        break;
                    }
                }
            }
        }
        finally
        {
            frontier.Writer.TryComplete();

            // Iptalde ya da hatada ucustaki getirmeler beklenmez, dusurulur.
            if (pending > 0) await stop.CancelAsync();
            await DrainAsync(workers);
        }

        if (pages.Count == 0)
        {
            // Tek sayfa bile cekilmeden iptal edildiyse bu bir hata degil.
            if (cancelled) return CrawlStatus.Cancelled;
            throw new InvalidOperationException("Hiçbir sayfa taranamadı — base_url geçersiz olabilir");
        }

        var htmlPages = pages.Count;
        await PersistPagesAsync(pages, saved, crawl, htmlPages, ct);

        // Ikili varliklar: yalniz durum yoklamasi. Sayfa butcesinden dusmez, kural
        // calistirilmaz — ama page_links hedefi olarak cozulur ki kirik link yakalansin.
        pages.AddRange(await ProbeAssetsAsync(
            crawl.Id, assets, settings.MaxAssetChecks, concurrency, pacer, ct));
        await PersistPagesAsync(pages, htmlPages, crawl, htmlPages, ct);

        ResolveLinkGraph(pages, links);
        AssignDepths(pages, links, seedIds);
        await repository.AddPageLinksAsync(links, ct);

        var home = pages.FirstOrDefault(p => p.Url == baseUri.AbsoluteUri) ?? pages[0];
        var vitals = await MeasureVitalsAsync(crawl, site, home, ct);

        var siteFacts = new CrawlSiteFacts
        {
            SitemapFound = seed.SitemapFound,
            SitemapUrls = seed.SitemapUrls,
            BlockedUrls = blocked,
            Vitals = vitals,
            HomePageId = home.Id
        };

        var crawlFindings = RunCrawlRules(pages, htmlPages, links, home, siteFacts);
        WriteIssues(crawl, site, pageFindings, crawlFindings);

        var allFindings = pageFindings.Select(f => f.Finding)
            .Concat(crawlFindings.Select(f => f.Finding))
            .ToList();

        // Varlik satirlari sayfa sayilmaz — ne butceden duser ne skoru sulandirir.
        crawl.PagesDiscovered = seen.Count;
        crawl.PagesCrawled = htmlPages;
        crawl.OverallScore = ruleRunner.OverallScore(pageScores, crawlFindings.Select(f => f.Finding));
        crawl.CategoryScores = ruleRunner.CategoryScores(allFindings, htmlPages);
        crawl.ScoringSnapshot = ruleRunner.ScoringSnapshot();
        crawl.IssueCounts = crawl.Issues
            .GroupBy(i => i.Severity.ToString().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        await repository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Crawl {CrawlId} bitti: {Crawled}/{Discovered} sayfa, {Links} link, {Issues} bulgu, skor {Score}",
            crawl.Id, crawl.PagesCrawled, crawl.PagesDiscovered, links.Count, crawl.Issues.Count, crawl.OverallScore);

        if (cancelled) return CrawlStatus.Cancelled;
        return truncated ? CrawlStatus.Partial : CrawlStatus.Completed;
    }

    // --- tohumlama ---

    private async Task<Seed> SeedFrontierAsync(
        Uri baseUri, IRobotsPolicy robots,
        IReadOnlyList<Regex> include, IReadOnlyList<Regex> exclude,
        HashSet<string> seen, HashSet<string> assetSeen, List<FrontierItem> assets,
        HashSet<string> blocked, int maxPages, CancellationToken ct)
    {
        var frontier = new List<FrontierItem>();

        // Kok URL robots/desen filtrelerinden muaf — siteyi hic taramamak anlamsiz olurdu.
        // Yine de robots kok sayfayi kapatiyorsa bu basli basina bir bulgudur.
        if (!robots.IsAllowed(baseUri)) blocked.Add(baseUri.AbsoluteUri);
        if (seen.Add(baseUri.AbsoluteUri))
            frontier.Add(new FrontierItem(baseUri, 0));

        var sitemapUrls = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in await ReadSitemapsAsync(robots, baseUri, ct))
        {
            if (!UrlNormalizer.TryNormalize(raw, baseUri, out var url)) continue;

            // Desen filtreleri disinda kalsa bile sitemap'te gecmis sayilir.
            sitemapUrls.Add(url.AbsoluteUri);

            if (UrlNormalizer.IsLikelyAsset(url))
            {
                if (TryAccept(url, baseUri, robots, include, exclude, assetSeen, blocked))
                    assets.Add(new FrontierItem(url, 0));
                continue;
            }

            if (frontier.Count >= maxPages) continue;
            if (TryAccept(url, baseUri, robots, include, exclude, seen, blocked))
                frontier.Add(new FrontierItem(url, 0));
        }

        return new Seed(frontier, sitemapUrls, sitemapUrls.Count > 0);
    }

    private async Task<IReadOnlyList<string>> ReadSitemapsAsync(
        IRobotsPolicy robots, Uri baseUri, CancellationToken ct)
    {
        var sitemaps = robots.Sitemaps.Count > 0
            ? robots.Sitemaps
            : [new Uri(baseUri, "/sitemap.xml").AbsoluteUri];

        var urls = new List<string>();
        foreach (var sitemap in sitemaps)
        {
            try
            {
                urls.AddRange(await sitemapSource.ReadAsync(sitemap, ct));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Sitemap okunamadi: {Sitemap}", sitemap);
            }
        }

        return urls;
    }

    // --- performans olcumu ---

    /// <summary>
    /// Kok sayfa icin PSI olcumu alir, vitals satirini yazar ve performans kurallarini besler.
    /// API anahtari yoksa ya da PSI hata verirse crawl etkilenmez — olcum atlanir.
    /// </summary>
    private async Task<VitalsFacts?> MeasureVitalsAsync(Crawl crawl, Site site, Page home, CancellationToken ct)
    {
        if (!pageSpeed.IsConfigured) return null;

        PageSpeedResult result;
        try
        {
            result = await pageSpeed.AnalyzeAsync(home.Url, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PSI olcumu alinamadi: {Url}", home.Url);
            return null;
        }

        await repository.AddVitalAsync(new Vital
        {
            SiteId = site.Id,
            CrawlId = crawl.Id,
            Url = home.Url,
            Device = VitalsDevice.Mobile,
            Source = VitalsSource.PsiLab,
            LcpMs = Round(result.LargestContentfulPaintMs),
            InpMs = Round(result.InteractionToNextPaintMs),
            Cls = result.CumulativeLayoutShift is double cls ? (decimal)cls : null,
            TtfbMs = Round(result.TimeToFirstByteMs),
            FcpMs = Round(result.FirstContentfulPaintMs),
            PerfScore = result.Performance
        }, ct);

        return new VitalsFacts(
            result.LargestContentfulPaintMs,
            result.CumulativeLayoutShift,
            result.InteractionToNextPaintMs);
    }

    private static int? Round(double? value) => value is double v ? (int)Math.Round(v) : null;

    // --- getirme ---

    /// <summary>
    /// Bir getirici: kuyruk kapanana kadar URL alir, getirir, sonucu tuketiciye yollar.
    /// Sayfa hatalari <see cref="SafeExtractAsync"/> icinde yutulur — buradan yalniz iptal cikar.
    /// </summary>
    private async Task FetchLoopAsync(
        ChannelReader<FrontierItem> frontier,
        ChannelWriter<(FrontierItem Item, ExtractedPage Page)> fetched,
        PageFetchOptions options, IRequestPacer pacer, CancellationToken ct)
    {
        await foreach (var item in frontier.ReadAllAsync(ct))
        {
            await pacer.AcquireAsync(ct);
            var page = await SafeExtractAsync(item.Url, options, ct);
            await fetched.WriteAsync((item, page), ct);
        }
    }

    /// <summary>Gettiricilerin bitmesini bekler; iptal disinda bir sey firlatmalari beklenmez.</summary>
    private static async Task DrainAsync(Task[] workers)
    {
        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            // Iptal edilen crawl'da beklenen son.
        }
    }

    private async Task<ExtractedPage> SafeExtractAsync(Uri url, PageFetchOptions options, CancellationToken ct)
    {
        try
        {
            return await extractor.ExtractAsync(url, options, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{Url} getirilemedi", url);
            return ExtractedPage.Failed(url, 0);
        }
    }

    // --- frontier filtreleri ---

    /// <summary>
    /// URL taranabilir mi; kabul edilirse <paramref name="seen"/>'e eklenir.
    /// robots.txt yuzunden elenenler <paramref name="blocked"/>'a yazilir — BLOCKED_BY_ROBOTS_TXT
    /// bulgusu buradan beslenir.
    /// </summary>
    private static bool TryAccept(
        Uri url, Uri baseUri, IRobotsPolicy robots,
        IReadOnlyList<Regex> include, IReadOnlyList<Regex> exclude,
        HashSet<string> seen, HashSet<string> blocked)
    {
        var absolute = url.AbsoluteUri;
        if (absolute.Length > MaxUrlLength) return false;
        if (!UrlNormalizer.IsInternal(url, baseUri)) return false;
        if (!robots.IsAllowed(url))
        {
            if (blocked.Count < MaxBlockedTracked) blocked.Add(absolute);
            return false;
        }
        if (exclude.Any(r => IsMatch(r, absolute))) return false;
        if (include.Count > 0 && !include.Any(r => IsMatch(r, absolute))) return false;

        return seen.Add(absolute);
    }

    private static bool IsMatch(Regex regex, string value)
    {
        try
        {
            return regex.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static IReadOnlyList<Regex> Compile(IEnumerable<string> patterns)
    {
        var result = new List<Regex>();
        foreach (var pattern in patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern)) continue;
            try
            {
                result.Add(new Regex(pattern, RegexOptions.IgnoreCase, PatternTimeout));
            }
            catch (ArgumentException)
            {
                // Gecersiz desen crawl'i dusurmez, yok sayilir.
            }
        }
        return result;
    }

    private static Uri ResolveBaseUri(string baseUrl)
    {
        var normalized = UrlNormalizer.NormalizeSiteBaseUrl(baseUrl)
            ?? throw new InvalidOperationException($"Geçersiz site adresi: {baseUrl}");
        return new Uri(normalized + "/");
    }

    // --- entity uretimi ---

    private static Page BuildPage(Guid crawlId, FrontierItem item, ExtractedPage extracted) => new()
    {
        CrawlId = crawlId,
        Url = Clip(item.Url.AbsoluteUri, MaxUrlLength),
        UrlHash = UrlNormalizer.Hash(item.Url),
        Depth = item.Depth,
        StatusCode = extracted.StatusCode,
        ContentType = ClipOrNull(extracted.ContentType, 128),
        RedirectTo = ClipOrNull(extracted.RedirectTo, MaxUrlLength),
        ResponseTimeMs = extracted.ResponseTimeMs,
        HtmlSizeBytes = extracted.HtmlSizeBytes,
        Title = ClipOrNull(extracted.Title, 1024),
        TitleLength = extracted.Title?.Length,
        MetaDescription = ClipOrNull(extracted.MetaDescription, 2048),
        MetaDescLength = extracted.MetaDescription?.Length,
        H1Texts = [.. extracted.H1],
        H2Count = extracted.H2Count,
        WordCount = extracted.WordCount,
        CanonicalUrl = ClipOrNull(extracted.CanonicalUrl, MaxUrlLength),
        RobotsMeta = ClipOrNull(extracted.RobotsMeta, 128),
        OgData = extracted.OgDataJson,
        SchemaTypes = [.. extracted.SchemaTypes],
        ImagesTotal = extracted.ImagesTotal,
        ImagesNoAlt = extracted.ImagesNoAlt,
        ContentHash = extracted.ContentHash,
        MainText = extracted.MainText,
        Lang = ClipOrNull(extracted.Lang, 16)
    };

    /// <summary>Sayfanin linklerini page_links satirlarina cevirir; (ic, dis) sayilarini doner.</summary>
    private static (int Internal, int External) CollectLinks(
        Guid crawlId, Page page, ExtractedPage extracted, List<PageLink> sink)
    {
        var internalCount = 0;
        var externalCount = 0;
        var targets = new HashSet<string>(StringComparer.Ordinal);

        foreach (var link in extracted.Links)
        {
            var target = link.Url.AbsoluteUri;
            if (target.Length > MaxUrlLength) continue;
            if (!targets.Add(target)) continue;

            if (link.IsInternal) internalCount++;
            else externalCount++;

            sink.Add(new PageLink
            {
                CrawlId = crawlId,
                FromPageId = page.Id,
                ToUrl = target,
                AnchorText = ClipOrNull(link.AnchorText, MaxAnchorLength),
                IsInternal = link.IsInternal,
                IsNofollow = link.IsNofollow
            });
        }

        return (internalCount, externalCount);
    }

    /// <summary>
    /// page_links.to_page_id'yi cozer, pages.inlink_count'u hesaplar.
    ///
    /// Hedef yonlendiriyorsa zincir sonuna kadar izlenir: <c>/Products</c> → <c>/products</c>
    /// gibi bir atlamada linkler yonlendirme satirinda birikirse gercek sayfa sifir inlink'le
    /// kalir ve ORPHAN_PAGE gibi kurallar yanlis tetiklenir. to_url linkte ne yaziyorsa odur;
    /// degisen yalniz hangi sayfaya sayildigi.
    /// </summary>
    private static void ResolveLinkGraph(List<Page> pages, List<PageLink> links)
    {
        var byHash = new Dictionary<string, Page>(StringComparer.Ordinal);
        var byUrl = new Dictionary<string, Page>(StringComparer.Ordinal);
        foreach (var page in pages)
        {
            byHash[Convert.ToHexString(page.UrlHash)] = page;
            byUrl[page.Url] = page;
        }

        var inlinks = new Dictionary<Guid, int>();
        foreach (var link in links)
        {
            if (!link.IsInternal) continue;
            if (!byHash.TryGetValue(Convert.ToHexString(UrlNormalizer.Hash(link.ToUrl)), out var target)) continue;

            target = FollowRedirects(target, byUrl);
            link.ToPageId = target.Id;
            inlinks[target.Id] = inlinks.GetValueOrDefault(target.Id) + 1;
        }

        foreach (var page in pages)
            page.InlinkCount = inlinks.GetValueOrDefault(page.Id);
    }

    /// <summary>
    /// Yonlendirme zincirinin sonundaki sayfa. Hedef taranmamissa ya da zincir donuyorsa
    /// elde kalan son sayfa donulur.
    /// </summary>
    private static Page FollowRedirects(Page page, Dictionary<string, Page> byUrl)
    {
        var current = page;
        for (var hop = 0; hop < MaxRedirectHops; hop++)
        {
            if (current.RedirectTo is not string next) break;
            if (!byUrl.TryGetValue(next, out var target) || target.Id == current.Id) break;
            current = target;
        }
        return current;
    }

    /// <summary>
    /// pages.depth = tohumlardan link grafigi uzerindeki en kisa mesafe.
    ///
    /// Getirme sirasindan hesaplanamaz: getiriciler paralel calistigi icin bir URL'i once
    /// hangi ebeveynin sonucunun kesfettigi yanit surelerine baglidir, o yuzden derinlik
    /// oldugundan buyuk cikabilirdi. Grafik uzerinden BFS hem es zamanliliktan bagimsiz
    /// hem de tanimi geregi dogru sonucu verir.
    ///
    /// Grafikte hedefi cozulmemis satirlar (dis linkler, taranmamis URL'ler) atlanir;
    /// ulasilamayan sayfa getirme sirasindaki derinligini korur.
    /// </summary>
    private static void AssignDepths(List<Page> pages, List<PageLink> links, HashSet<Guid> seedIds)
    {
        var outgoing = new Dictionary<Guid, List<Guid>>();
        foreach (var link in links)
        {
            if (link.ToPageId is not Guid target) continue;
            if (!outgoing.TryGetValue(link.FromPageId, out var targets))
                outgoing[link.FromPageId] = targets = [];
            targets.Add(target);
        }

        var depths = new Dictionary<Guid, int>();
        var queue = new Queue<Guid>();
        foreach (var id in seedIds)
        {
            depths[id] = 0;
            queue.Enqueue(id);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!outgoing.TryGetValue(current, out var targets)) continue;

            var next = depths[current] + 1;
            foreach (var target in targets)
            {
                // Ilk ulasan en kisa olandir — BFS.
                if (!depths.TryAdd(target, next)) continue;
                queue.Enqueue(target);
            }
        }

        foreach (var page in pages)
            if (depths.TryGetValue(page.Id, out var depth)) page.Depth = depth;
    }

    /// <summary>
    /// <paramref name="pages"/> varlik satirlarini da icerir; kural girdisi yalniz ilk
    /// <paramref name="htmlCount"/> HTML sayfasidir. Varlik durumlari link cozumu icin gerekli.
    /// </summary>
    private IReadOnlyList<CrawlRuleFinding> RunCrawlRules(
        List<Page> pages, int htmlCount, List<PageLink> links, Page home, CrawlSiteFacts site)
    {
        var statusById = pages.ToDictionary(p => p.Id, p => p.StatusCode);
        var urlById = pages.ToDictionary(p => p.Id, p => p.Url);

        var pageFacts = pages
            .Take(htmlCount)
            // Yonlendirilen URL'in govdesi hedefe ait. Girdide birakilirsa ayni belge iki kez
            // sayilir ve DUPLICATE_CONTENT / META_TITLE_DUPLICATE kendi hedefiyle eslesir.
            .Where(p => p.RedirectTo is null)
            .Select(p => new CrawlPageFacts(p.Id, p.Url, p.StatusCode, p.ContentHash)
            {
                Depth = p.Depth,
                InlinkCount = p.InlinkCount,
                Title = p.Title,
                MetaDescription = p.MetaDescription,
                IsHome = p.Id == home.Id,
                NoIndex = HasNoIndex(p.RobotsMeta)
            })
            .ToList();

        var linkFacts = links
            .Select(l => new CrawlLinkFacts(
                l.FromPageId,
                urlById.GetValueOrDefault(l.FromPageId, string.Empty),
                l.ToUrl,
                l.IsInternal,
                l.ToPageId is Guid id && statusById.TryGetValue(id, out var status) ? status : null))
            .ToList();

        return ruleRunner.RunCrawl(pageFacts, linkFacts, site);
    }

    private static bool HasNoIndex(string? robotsMeta) =>
        robotsMeta?.Contains("noindex", StringComparison.OrdinalIgnoreCase) == true;

    private static void WriteIssues(
        Crawl crawl, Site site,
        List<(Page Page, RuleFinding Finding)> pageFindings,
        IReadOnlyList<CrawlRuleFinding> crawlFindings)
    {
        foreach (var (page, finding) in pageFindings)
            crawl.Issues.Add(NewIssue(crawl, site, page.Id, finding));

        foreach (var crawlFinding in crawlFindings)
            crawl.Issues.Add(NewIssue(crawl, site, crawlFinding.PageId, crawlFinding.Finding));
    }

    private static Issue NewIssue(Crawl crawl, Site site, Guid? pageId, RuleFinding finding) => new()
    {
        CrawlId = crawl.Id,
        SiteId = site.Id,
        PageId = pageId,
        RuleCode = finding.RuleCode,
        Severity = finding.Severity,
        Weight = finding.Weight,
        Evidence = new IssueEvidence
        {
            Found = finding.Message,
            SampleUrls = [.. finding.SampleUrls]
        },
        Status = IssueStatus.Open,
        FirstSeenCrawlId = crawl.Id
    };

    // --- kalicilik ---

    /// <summary>
    /// Henuz yazilmamis satirlari kaydeder; yeni imleci doner. <paramref name="progress"/>
    /// crawl.pages_crawled'a yazilir — varlik satirlari buna dahil degildir.
    /// </summary>
    private async Task<int> PersistPagesAsync(
        List<Page> pages, int from, Crawl crawl, int progress, CancellationToken ct)
    {
        if (from >= pages.Count) return from;

        await repository.AddPagesAsync(pages.Skip(from).ToList(), ct);
        crawl.PagesCrawled = progress;
        await repository.SaveChangesAsync(ct);
        return pages.Count;
    }

    /// <summary>
    /// Ikili varliklarin yalniz durumunu yoklar (HEAD). Sayfa alanlari bos kalir;
    /// amac page_links hedeflerinin cozulmesi ve kirik varlik linklerinin yakalanmasi.
    /// </summary>
    private async Task<List<Page>> ProbeAssetsAsync(
        Guid crawlId, List<FrontierItem> assets, int maxChecks, int concurrency,
        IRequestPacer pacer, CancellationToken ct)
    {
        if (maxChecks <= 0 || assets.Count == 0) return [];

        var batch = assets.Count > maxChecks ? assets.Take(maxChecks).ToList() : assets;
        var probed = new ConcurrentBag<Page>();

        await Parallel.ForEachAsync(
            batch,
            new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = ct },
            async (item, token) =>
            {
                await pacer.AcquireAsync(token);

                ExtractedPage result;
                try
                {
                    result = await extractor.ProbeAsync(item.Url, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "{Url} varligi yoklanamadi", item.Url);
                    result = ExtractedPage.Failed(item.Url, 0);
                }

                probed.Add(BuildPage(crawlId, item, result));
            });

        logger.LogInformation(
            "Crawl {CrawlId}: {Probed}/{Total} ikili varlik yoklandi", crawlId, batch.Count, assets.Count);

        return [.. probed];
    }

    private static string Clip(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string? ClipOrNull(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
