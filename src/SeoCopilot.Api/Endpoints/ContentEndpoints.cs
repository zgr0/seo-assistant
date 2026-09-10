using System.Security.Claims;
using SeoCopilot.Api.Infrastructure;
using SeoCopilot.Application.Common;
using SeoCopilot.Application.Dtos;
using SeoCopilot.Application.Services;

namespace SeoCopilot.Api.Endpoints;

public static class BrandProfileEndpoints
{
    public static IEndpointRouteBuilder MapBrandProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var brands = app.MapGroup("/api/brand-profiles").WithTags("BrandProfiles").RequireAuthorization();

        brands.MapGet("/", async (
            ClaimsPrincipal user, BrandProfileService service, Guid? siteId, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(user.TenantId(), siteId, ct)));

        brands.MapGet("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, BrandProfileService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(id, user.TenantId(), ct)));

        brands.MapPost("/", async (
            CreateBrandProfileRequest req, ClaimsPrincipal user,
            BrandProfileService service, CancellationToken ct) =>
        {
            var profile = await service.CreateAsync(req, user.TenantId(), ct);
            return Results.Created($"/api/brand-profiles/{profile.Id}", profile);
        });

        brands.MapPatch("/{id:guid}", async (
            Guid id, UpdateBrandProfileRequest req, ClaimsPrincipal user,
            BrandProfileService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateAsync(id, user.TenantId(), req, ct)));

        // Seed listesi — kiraciya gore degismez.
        app.MapGet("/api/platform-profiles", async (
                BrandProfileService service, bool? includeInactive, CancellationToken ct) =>
                Results.Ok(await service.ListPlatformsAsync(includeInactive is not true, ct)))
            .WithTags("BrandProfiles")
            .RequireAuthorization();

        return app;
    }
}

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/content").WithTags("Content").RequireAuthorization();

        group.MapPost("/generate", async (
            GenerateContentRequest req, ClaimsPrincipal user, ContentService content, CancellationToken ct) =>
        {
            var job = await content.CreateAsync(req, user.TenantId(), user.UserId(), ct);
            return Results.Created($"/api/content/jobs/{job.Id}", job);
        });

        group.MapPost("/generate-batch", async (
            GenerateBatchRequest req, ClaimsPrincipal user, ContentService content, CancellationToken ct) =>
        {
            var result = await content.CreateBatchAsync(req, user.TenantId(), user.UserId(), ct);
            return Results.Accepted("/api/content/jobs", result);
        });

        group.MapGet("/jobs/{jobId:guid}", async (
            Guid jobId, ClaimsPrincipal user, ContentService content, CancellationToken ct) =>
        {
            var job = await content.GetJobAsync(jobId, user.TenantId(), ct);
            return job is null ? Results.NotFound() : Results.Ok(job);
        });

        group.MapGet("/jobs", async (
            ClaimsPrincipal user, ContentService content,
            string? type, string? platformCode, string? status, Guid? siteId, Guid? pageId,
            int? page, int? size, CancellationToken ct) =>
            Results.Ok(await content.ListJobsAsync(
                user.TenantId(), type, platformCode, status, siteId, pageId,
                page ?? 1, size ?? Paging.DefaultSize, ct)));

        group.MapPost("/variants/{variantId:guid}/favorite", async (
            Guid variantId, FavoriteVariantRequest? req, ClaimsPrincipal user,
            ContentService content, CancellationToken ct) =>
            Results.Ok(await content.SetFavoriteAsync(variantId, user.TenantId(), req?.IsFavorite, ct)));

        group.MapGet("/assets/{assetId:guid}", async (
            Guid assetId, ClaimsPrincipal user, ContentService content,
            HttpContext http, CancellationToken ct) =>
        {
            var (bytes, contentType) = await content.GetAssetAsync(assetId, user.TenantId(), ct);
            // Icerik degismez (yeni uretim yeni kimlik alir) — tarayici onbellegine birak.
            http.Response.Headers.CacheControl = "private, max-age=86400";
            return Results.File(bytes, contentType);
        });

        group.MapGet("/export.csv", async (
            ClaimsPrincipal user, ContentService content, string? jobIds, CancellationToken ct) =>
        {
            var ids = ParseIds(jobIds);
            var csv = await content.ExportCsvAsync(user.TenantId(), ids, ct);
            return Results.File(csv, "text/csv; charset=utf-8", "seocopilot-icerik.csv");
        });

        return app;
    }

    /// <summary>?jobIds=a,b,c — gecersiz kimlikler yok sayilir.</summary>
    private static List<Guid> ParseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("jobIds parametresi zorunlu");

        return [.. raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => Guid.TryParse(part, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()];
    }
}

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization();

        group.MapGet("/{reportId:guid}", async (
            Guid reportId, ClaimsPrincipal user, ReportService reports, CancellationToken ct) =>
        {
            var report = await reports.GetAsync(reportId, user.TenantId(), ct);
            return report is null ? Results.NotFound() : Results.Ok(report);
        });

        group.MapGet("/{reportId:guid}/download", async (
            Guid reportId, ClaimsPrincipal user, ReportService reports, CancellationToken ct) =>
        {
            var (content, fileName) = await reports.DownloadAsync(reportId, user.TenantId(), ct);
            return Results.File(content, ReportService.ContentType, fileName);
        });

        return app;
    }
}

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (
                ClaimsPrincipal user, DashboardService dashboard, CancellationToken ct) =>
                Results.Ok(await dashboard.GetAsync(user.TenantId(), ct)))
            .WithTags("Dashboard")
            .RequireAuthorization();

        return app;
    }
}
