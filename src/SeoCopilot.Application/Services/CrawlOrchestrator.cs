using System.Security.Cryptography;
using System.Text;
using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Crawling;
using SeoCopilot.Domain.Entities.Json;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>
/// Ana use-case: dogrulanmis bir site icin crawl kuyruga at; worker cagrisinda
/// sayfalari cek, kural motorunu calistir, issue'lari ve skoru yaz.
/// </summary>
public sealed class CrawlOrchestrator(
    ISiteRepository repository,
    IPageExtractor extractor,
    IRuleRunner ruleRunner,
    ICrawlQueue queue)
{
    public async Task<StartCrawlResponse> StartAsync(StartCrawlRequest request, CancellationToken ct = default)
    {
        var site = await repository.GetSiteAsync(request.SiteId, ct)
            ?? throw new InvalidOperationException($"Site {request.SiteId} bulunamadi");

        if (site.VerifiedAt is null)
            throw new InvalidOperationException("Site dogrulanmadan tarama baslatilamaz");

        var crawl = new Crawl
        {
            SiteId = site.Id,
            Status = CrawlStatus.Queued,
            Trigger = CrawlTrigger.Manual
        };

        await repository.AddCrawlAsync(crawl, ct);
        await repository.SaveChangesAsync(ct);

        queue.Enqueue(crawl.Id);
        return new StartCrawlResponse(crawl.Id);
    }

    /// <summary>Hangfire worker tarafindan cagrilir.</summary>
    public async Task RunAsync(Guid crawlId, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlAsync(crawlId, ct)
            ?? throw new InvalidOperationException($"Crawl {crawlId} bulunamadi");
        var site = await repository.GetSiteAsync(crawl.SiteId, ct)
            ?? throw new InvalidOperationException($"Site {crawl.SiteId} bulunamadi");

        crawl.Status = CrawlStatus.Running;
        crawl.StartedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);

        try
        {
            var extracted = await extractor.ExtractAsync(site.BaseUrl, ct);

            var page = new Page
            {
                CrawlId = crawl.Id,
                Url = extracted.Url,
                UrlHash = Sha256(extracted.Url),
                Depth = 0,
                StatusCode = extracted.StatusCode,
                Title = extracted.Title,
                TitleLength = extracted.Title?.Length,
                MetaDescription = extracted.MetaDescription,
                MetaDescLength = extracted.MetaDescription?.Length,
                H1Texts = [.. extracted.H1],
                WordCount = extracted.WordCount,
                OutlinkInternal = extracted.InternalLinks.Count
            };
            crawl.Pages.Add(page);

            var outcome = ruleRunner.Run(extracted);
            foreach (var f in outcome.Findings)
            {
                crawl.Issues.Add(new Issue
                {
                    CrawlId = crawl.Id,
                    SiteId = site.Id,
                    PageId = page.Id,
                    RuleCode = f.RuleCode,
                    Severity = f.Severity,
                    Weight = (int)f.Severity,
                    Evidence = new IssueEvidence { Found = f.Message },
                    Status = IssueStatus.Open,
                    FirstSeenCrawlId = crawl.Id
                });
            }

            crawl.IssueCounts = crawl.Issues
                .GroupBy(i => i.Severity.ToString().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count());
            crawl.OverallScore = outcome.Score;
            crawl.PagesDiscovered = 1;
            crawl.PagesCrawled = 1;
            crawl.Status = CrawlStatus.Completed;
            crawl.FinishedAt = DateTimeOffset.UtcNow;

            await repository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            crawl.Status = CrawlStatus.Failed;
            crawl.ErrorMessage = ex.Message;
            crawl.FinishedAt = DateTimeOffset.UtcNow;
            await repository.SaveChangesAsync(ct);
            throw;
        }
    }

    public async Task<CrawlSummaryDto?> GetSummaryAsync(Guid crawlId, CancellationToken ct = default)
    {
        var crawl = await repository.GetCrawlAsync(crawlId, ct);
        return crawl is null ? null : CrawlSummaryDto.From(crawl);
    }

    private static byte[] Sha256(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
