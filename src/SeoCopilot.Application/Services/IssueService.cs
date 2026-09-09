using SeoCopilot.Application.Abstractions;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Domain.Entities.Rules;
using SeoCopilot.Domain.Enums;

namespace SeoCopilot.Application.Services;

/// <summary>Bulgu yasam dongusu: gormezden gelme ve geri acma.</summary>
public sealed class IssueService(ISiteRepository repository)
{
    /// <summary>
    /// Bulguyu kapatir ve bir <c>issue_ignores</c> kaydi yazar. <c>applyToSite</c> true ise
    /// kural site genelinde susturulur ve ayni koda sahip diger acik bulgular da kapatilir;
    /// false ise yalniz bu bulgunun URL'i icin gecerli olur.
    /// </summary>
    public async Task<IssueDto> IgnoreAsync(
        long issueId, Guid tenantId, Guid userId, IgnoreIssueRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Görmezden gelme gerekçesi zorunlu");

        var issue = await repository.GetIssueForTenantAsync(issueId, tenantId, ct)
            ?? throw new NotFoundException($"Bulgu {issueId} bulunamadı");

        var urlPattern = request.ApplyToSite ? null : issue.Page?.Url;
        var reason = request.Reason.Trim();

        var existing = await repository.FindIssueIgnoreAsync(issue.SiteId, issue.RuleCode, urlPattern, ct);
        if (existing is null)
        {
            await repository.AddIssueIgnoreAsync(new IssueIgnore
            {
                SiteId = issue.SiteId,
                RuleCode = issue.RuleCode,
                UrlPattern = urlPattern,
                Reason = reason,
                CreatedBy = userId
            }, ct);
        }
        else
        {
            existing.Reason = reason;
        }

        issue.Status = IssueStatus.Ignored;

        if (request.ApplyToSite)
        {
            foreach (var other in await repository.ListSiteIssuesByRuleAsync(issue.SiteId, issue.RuleCode, ct))
                other.Status = IssueStatus.Ignored;
        }

        await repository.SaveChangesAsync(ct);
        return IssueDto.From(issue);
    }

    /// <summary>
    /// Bulguyu yeniden acar ve onu susturan kaydi kaldirir. Site geneli bir kayit varsa o da
    /// silinir; ancak ayni kuralla kapatilmis diger bulgular otomatik geri acilmaz.
    /// </summary>
    public async Task<IssueDto> ReopenAsync(long issueId, Guid tenantId, CancellationToken ct = default)
    {
        var issue = await repository.GetIssueForTenantAsync(issueId, tenantId, ct)
            ?? throw new NotFoundException($"Bulgu {issueId} bulunamadı");

        if (issue.Page?.Url is string url)
        {
            var forUrl = await repository.FindIssueIgnoreAsync(issue.SiteId, issue.RuleCode, url, ct);
            if (forUrl is not null) await repository.RemoveIssueIgnoreAsync(forUrl, ct);
        }

        var siteWide = await repository.FindIssueIgnoreAsync(issue.SiteId, issue.RuleCode, null, ct);
        if (siteWide is not null) await repository.RemoveIssueIgnoreAsync(siteWide, ct);

        issue.Status = IssueStatus.Open;
        issue.ResolvedCrawlId = null;

        await repository.SaveChangesAsync(ct);
        return IssueDto.From(issue);
    }
}
