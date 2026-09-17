using System.Text;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler;

/// <summary>
/// Rapor PDF'ini headless Chromium ile basar.
///
/// Crawler projesinde duruyor cunku tarayici burada: <see cref="PlaywrightBrowserPool"/>
/// singleton'i tarama ile paylasilir, ayri bir Chromium acilmaz. Her cagri kendi
/// context'ini aldigi icin tarama isleriyle durum paylasmaz.
/// </summary>
public sealed class PlaywrightPdfRenderer(PlaywrightBrowserPool browsers) : IPdfRenderer
{
    public Task<byte[]> RenderAsync(byte[] html, CancellationToken ct = default) =>
        browsers.PrintPdfAsync(Encoding.UTF8.GetString(html), ct);
}
