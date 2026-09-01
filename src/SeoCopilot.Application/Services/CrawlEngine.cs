using System.Collections.Concurrent;
using System.Text.RegularExpressions;
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
/// Cok sayfali tarama motoru. Seviye seviye BFS: her seviye <see cref="CrawlSettings.Concurrency"/>
/// paralellikte getirilir, her getirmeden sonra <see cref="CrawlSettings.DelayMs"/> beklenir.
/// Getirmeler paralel, veritabanina yazma hep tek is parcaciginda (DbContext thread-safe degil).
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

    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

    private sealed record FrontierItem(Uri Url, int Depth);

    /// <summary>Tohumlama ciktisi: baslangic kuyrugu ve sitemap gercekleri.</summary>
    private sealed record Seed(List<FrontierItem> Frontier, HashSet<string> SitemapUrls, bool SitemapFound);

    /// <summary>
    /// Crawl'i bastan sona yurutur; pages / page_links / issues satirlarini ve crawl ozet
    /// alanlarini yazar. Durumu doner — StartedAt/FinishedAt cagirana ait.
    /// </summary>
    /// <param name="cancelRequested">
    /// Her derinlik gecisinde sorulur; true donerse tarama o ana kadarki verilerle kapatilir
    /// ve <see cref="CrawlStatus.Cancelled"/> donulur. Kullanicinin iptal istegi buradan gelir.
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
        var fetchOptions = new PageFetchOptions(baseUri, settings.RenderJs);

        var include = Compile(settings.IncludePatterns);
        var exclude = Compile(settings.ExcludePatterns);
        var robots = await robotsSource.GetAsync(baseUri, ct);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var blocked = new HashSet<string>(StringComparer.Ordinal);
        var pages = new List<Page>();
        var links = new List<PageLink>();
        var pageFindings = new List<(Page Page, RuleFinding Finding)>();
        var pageScores = new List<int>();
        var truncated = false;
        var cancelled = false;
        var saved = 0;

        var seed = await SeedFrontierAsync(baseUri, robots, include, exclude, seen, blocked, maxPages, ct);
        var frontier = seed.Frontier;
        logger.LogInformation(
            "Crawl {CrawlId} basliyor: {Seed} tohum URL, maxPages={MaxPages} maxDepth={MaxDepth} concurrency={Concurrency}",
            crawl.Id, frontier.Count, maxPages, maxDepth, concurrency);

        for (var depth = 0; depth <= maxDepth && frontier.Count > 0; depth++)
        {
            ct.ThrowIfCancellationRequested();

            if (cancelRequested is not null && await cancelRequested(ct))
            {
                logger.LogInformation("Crawl {CrawlId} kullanici tarafindan iptal edildi", crawl.Id);
                cancelled = true;
                break;
            }

            var remaining = maxPages - pages.Count;
            if (remaining <= 0)
            {
                truncated = true;
                break;
            }

            var batch = frontier;
            if (batch.Count > remaining)
            {
                batch = [.. frontier.Take(remaining)];
                truncated = true;
            }

            var fetched = new ConcurrentBag<(FrontierItem Item, ExtractedPage Page)>();
            await Parallel.ForEachAsync(
                batch,
                new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = ct },
                async (item, token) =>
                {
                    fetched.Add((item, await SafeExtractAsync(item.Url, fetchOptions, token)));
                    if (delayMs > 0) await Task.Delay(delayMs, token);
                });

            // Buradan sonrasi tek is parcacigi: entity uretimi, kural calistirma, kayit.
            var next = new List<FrontierItem>();
            foreach (var (item, extracted) in fetched)
            {
                var page = BuildPage(crawl.Id, item, extracted);
                pages.Add(page);

                var outcome = ruleRunner.Run(extracted);
                pageScores.Add(outcome.Score);
                foreach (var finding in outcome.Findings)
                    pageFindings.Add((page, finding));

                var (internalCount, externalCount) = CollectLinks(crawl.Id, page, extracted, links);
                page.OutlinkInternal = internalCount;
                page.OutlinkExternal = externalCount;

                foreach (var link in extracted.Links)
                {
                    if (!link.IsInternal || link.IsNofollow) continue;
                    if (!TryAccept(link.Url, baseUri, robots, include, exclude, seen, blocked)) continue;
                    next.Add(new FrontierItem(link.Url, depth + 1));
                }
            }

            if (pages.Count - saved >= SaveBatchSize)
                saved = await PersistPagesAsync(pages, saved, crawl, ct);

            logger.LogInformation(
                "Crawl {CrawlId} derinlik {Depth}: {Fetched} sayfa cekildi, {Next} yeni URL kuyruga girdi",
                crawl.Id, depth, fetched.Count, next.Count);

            frontier = next;
        }

        // Derinlik/sayfa tavani yuzunden kuyrukta URL kaldiysa tarama tam degil.
        if (frontier.Count > 0) truncated = true;

        if (pages.Count == 0)
        {
            // Tek sayfa bile cekilmeden iptal edildiyse bu bir hata degil.
            if (cancelled) return CrawlStatus.Cancelled;
            throw new InvalidOperationException("Hicbir sayfa taranamadi — base_url gecersiz olabilir");
        }

        await PersistPagesAsync(pages, saved, crawl, ct);

        ResolveLinkGraph(pages, links);
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

        var crawlFindings = RunCrawlRules(pages, links, home, siteFacts);
        WriteIssues(crawl, site, pageFindings, crawlFindings);

        var allFindings = pageFindings.Select(f => f.Finding)
            .Concat(crawlFindings.Select(f => f.Finding))
            .ToList();

        crawl.PagesDiscovered = seen.Count;
        crawl.PagesCrawled = pages.Count;
        crawl.OverallScore = ruleRunner.OverallScore(pageScores, crawlFindings.Select(f => f.Finding));
        crawl.CategoryScores = ruleRunner.CategoryScores(allFindings, pages.Count);
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
        HashSet<string> seen, HashSet<string> blocked, int maxPages, CancellationToken ct)
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
            ?? throw new InvalidOperationException($"Gecersiz site adresi: {baseUrl}");
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

    /// <summary>page_links.to_page_id'yi cozer, pages.inlink_count'u hesaplar.</summary>
    private static void ResolveLinkGraph(List<Page> pages, List<PageLink> links)
    {
        var byHash = new Dictionary<string, Page>(StringComparer.Ordinal);
        foreach (var page in pages)
            byHash[Convert.ToHexString(page.UrlHash)] = page;

        var inlinks = new Dictionary<Guid, int>();
        foreach (var link in links)
        {
            if (!link.IsInternal) continue;
            if (!byHash.TryGetValue(Convert.ToHexString(UrlNormalizer.Hash(link.ToUrl)), out var target)) continue;

            link.ToPageId = target.Id;
            inlinks[target.Id] = inlinks.GetValueOrDefault(target.Id) + 1;
        }

        foreach (var page in pages)
            page.InlinkCount = inlinks.GetValueOrDefault(page.Id);
    }

    private IReadOnlyList<CrawlRuleFinding> RunCrawlRules(
        List<Page> pages, List<PageLink> links, Page home, CrawlSiteFacts site)
    {
        var statusById = pages.ToDictionary(p => p.Id, p => p.StatusCode);
        var urlById = pages.ToDictionary(p => p.Id, p => p.Url);

        var pageFacts = pages
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

    /// <summary>Henuz yazilmamis sayfalari kaydeder, ilerlemeyi crawl'a isler; yeni imleci doner.</summary>
    private async Task<int> PersistPagesAsync(List<Page> pages, int from, Crawl crawl, CancellationToken ct)
    {
        if (from >= pages.Count) return from;

        await repository.AddPagesAsync(pages.Skip(from).ToList(), ct);
        crawl.PagesCrawled = pages.Count;
        await repository.SaveChangesAsync(ct);
        return pages.Count;
    }

    private static string Clip(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string? ClipOrNull(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
