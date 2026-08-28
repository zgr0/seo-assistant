using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities;

namespace SeoCopilot.Application.Services;

/// <summary>
/// Ana use-case: bir siteyi kaydet, crawl kuyruga at; worker cagrisinda sayfalari cek,
/// kural motorunu calistir, skoru yaz, rapor maili gonder.
/// </summary>
public sealed class CrawlOrchestrator(
    ISiteRepository repository,
    IPageExtractor extractor,
    IRuleRunner ruleRunner,
    ICrawlQueue queue,
    IEmailSender email)
{
    public async Task<StartCrawlResponse> StartAsync(StartCrawlRequest request, CancellationToken ct = default)
    {
        var site = new Site(request.Url, request.OwnerEmail);
        var crawl = site.StartCrawl();
        await repository.AddAsync(site, ct);
        await repository.SaveChangesAsync(ct);

        queue.Enqueue(crawl.Id);
        return new StartCrawlResponse(site.Id, crawl.Id);
    }

    /// <summary>Hangfire worker tarafindan cagrilir.</summary>
    public async Task RunAsync(Guid crawlId, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlAsync(crawlId, ct)
            ?? throw new InvalidOperationException($"Crawl {crawlId} bulunamadi");
        var site = await repository.GetAsync(crawl.SiteId, ct)
            ?? throw new InvalidOperationException($"Site {crawl.SiteId} bulunamadi");

        crawl.MarkRunning();
        await repository.SaveChangesAsync(ct);

        try
        {
            var extracted = await extractor.ExtractAsync(site.Url, ct);
            var page = crawl.AddPage(extracted.Url);
            page.SetResponse(extracted.StatusCode, extracted.Title, extracted.MetaDescription);

            var outcome = ruleRunner.Run(extracted);
            foreach (var f in outcome.Findings)
                page.AddFinding(new Finding(f.RuleCode, f.Severity, f.Message));

            crawl.Complete(outcome.Score);
            await repository.SaveChangesAsync(ct);

            await email.SendAsync(
                site.OwnerEmail,
                $"SEO raporu hazir: {site.Url}",
                $"<p>Skor: <b>{outcome.Score}/100</b> — {outcome.Findings.Count} bulgu.</p>",
                ct);
        }
        catch (Exception ex)
        {
            crawl.Fail(ex.Message);
            await repository.SaveChangesAsync(ct);
            throw;
        }
    }

    public async Task<CrawlSummaryDto?> GetSummaryAsync(Guid crawlId, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlAsync(crawlId, ct);
        return crawl is null ? null : CrawlSummaryDto.From(crawl);
    }
}
