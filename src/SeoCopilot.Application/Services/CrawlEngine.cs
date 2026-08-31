using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
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
    IRuleRunner ruleRunner,
    ILogger<CrawlEngine> logger)
{
    private const int SaveBatchSize = 50;
    private const int MaxUrlLength = 2048;
    private const int MaxAnchorLength = 512;
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

    private sealed record FrontierItem(Uri Url, int Depth);

    /// <summary>
    /// Crawl'i bastan sona yurutur; pages / page_links / issues satirlarini ve crawl ozet
    /// alanlarini yazar. Durumu doner — StartedAt/FinishedAt cagirana ait.
    /// </summary>
    public async Task<CrawlStatus> RunAsync(Crawl crawl, Site site, CancellationToken ct = default)
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
        var pages = new List<Page>();
        var links = new List<PageLink>();
        var pageFindings = new List<(Page Page, RuleFinding Finding)>();
        var pageScores = new List<int>();
        var truncated = false;
        var saved = 0;

        var frontier = await SeedFrontierAsync(baseUri, robots, include, exclude, seen, maxPages, ct);
        logger.LogInformation(
            "Crawl {CrawlId} basliyor: {Seed} tohum URL, maxPages={MaxPages} maxDepth={MaxDepth} concurrency={Concurrency}",
            crawl.Id, frontier.Count, maxPages, maxDepth, concurrency);

        for (var depth = 0; depth <= maxDepth && frontier.Count > 0; depth++)
        {
            ct.ThrowIfCancellationRequested();

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
                    if (!TryAccept(link.Url, baseUri, robots, include, exclude, seen)) continue;
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
            throw new InvalidOperationException("Hicbir sayfa taranamadi — base_url gecersiz olabilir");

        await PersistPagesAsync(pages, saved, crawl, ct);

        ResolveLinkGraph(pages, links);
        await repository.AddPageLinksAsync(links, ct);

        var crawlFindings = RunCrawlRules(pages, links);
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

        return truncated ? CrawlStatus.Partial : CrawlStatus.Completed;
    }

    // --- tohumlama ---

    private async Task<List<FrontierItem>> SeedFrontierAsync(
        Uri baseUri, IRobotsPolicy robots,
        IReadOnlyList<Regex> include, IReadOnlyList<Regex> exclude,
        HashSet<string> seen, int maxPages, CancellationToken ct)
    {
        var frontier = new List<FrontierItem>();

        // Kok URL robots/desen filtrelerinden muaf — siteyi hic taramamak anlamsiz olurdu.
        if (seen.Add(baseUri.AbsoluteUri))
            frontier.Add(new FrontierItem(baseUri, 0));

        foreach (var raw in await ReadSitemapsAsync(robots, baseUri, ct))
        {
            if (frontier.Count >= maxPages) break;
            if (UrlNormalizer.TryNormalize(raw, baseUri, out var url)
                && TryAccept(url, baseUri, robots, include, exclude, seen))
                frontier.Add(new FrontierItem(url, 0));
        }

        return frontier;
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

    /// <summary>URL taranabilir mi; kabul edilirse <paramref name="seen"/>'e eklenir.</summary>
    private static bool TryAccept(
        Uri url, Uri baseUri, IRobotsPolicy robots,
        IReadOnlyList<Regex> include, IReadOnlyList<Regex> exclude,
        HashSet<string> seen)
    {
        var absolute = url.AbsoluteUri;
        if (absolute.Length > MaxUrlLength) return false;
        if (!UrlNormalizer.IsInternal(url, baseUri)) return false;
        if (!robots.IsAllowed(url)) return false;
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

    private IReadOnlyList<CrawlRuleFinding> RunCrawlRules(List<Page> pages, List<PageLink> links)
    {
        var statusById = pages.ToDictionary(p => p.Id, p => p.StatusCode);
        var urlById = pages.ToDictionary(p => p.Id, p => p.Url);

        var pageFacts = pages
            .Select(p => new CrawlPageFacts(p.Id, p.Url, p.StatusCode, p.ContentHash))
            .ToList();

        var linkFacts = links
            .Select(l => new CrawlLinkFacts(
                l.FromPageId,
                urlById.GetValueOrDefault(l.FromPageId, string.Empty),
                l.ToUrl,
                l.IsInternal,
                l.ToPageId is Guid id && statusById.TryGetValue(id, out var status) ? status : null))
            .ToList();

        return ruleRunner.RunCrawl(pageFacts, linkFacts);
    }

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
