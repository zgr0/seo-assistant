using System.Text;
using System.Text.RegularExpressions;
using SeoCopilot.Application.Abstractions;

namespace SeoCopilot.Crawler;

/// <summary>
/// robots.txt ayristirici (RFC 9309). Ardisik User-agent satirlari tek grup basligi sayilir;
/// bizim token'imizi hedefleyen grup varsa o, yoksa "*" grubu uygulanir. Eslesmede en uzun
/// desen kazanir, esitlikte Allow oncelikli.
/// </summary>
public sealed class RobotsTxt : IRobotsPolicy
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    private sealed record Directive(bool Allow, string Pattern, Regex Matcher);

    private readonly List<Directive> _directives;
    private readonly List<string> _sitemaps;

    private RobotsTxt(List<Directive> directives, List<string> sitemaps)
    {
        _directives = directives;
        _sitemaps = sitemaps;
    }

    public IReadOnlyList<string> Sitemaps => _sitemaps;

    /// <summary>robots.txt yoksa veya okunamadiysa kullanilir.</summary>
    public static RobotsTxt AllowAll() => new([], []);

    public static RobotsTxt Parse(string content, string userAgentToken = "*")
    {
        var token = userAgentToken.Trim().ToLowerInvariant();
        var sitemaps = new List<string>();
        var groups = new List<(HashSet<string> Agents, List<Directive> Directives)>();

        (HashSet<string> Agents, List<Directive> Directives)? current = null;
        var inHeader = false;

        foreach (var raw in content.Split('\n'))
        {
            var line = raw.Trim();
            var comment = line.IndexOf('#');
            if (comment >= 0) line = line[..comment].Trim();
            if (line.Length == 0) continue;

            var idx = line.IndexOf(':');
            if (idx < 0) continue;
            var key = line[..idx].Trim().ToLowerInvariant();
            var value = line[(idx + 1)..].Trim();

            switch (key)
            {
                case "sitemap" when value.Length > 0:
                    sitemaps.Add(value);
                    break;

                case "user-agent" when value.Length > 0:
                    if (current is null || !inHeader)
                    {
                        current = (new HashSet<string>(StringComparer.OrdinalIgnoreCase), []);
                        groups.Add(current.Value);
                        inHeader = true;
                    }
                    current.Value.Agents.Add(value.ToLowerInvariant());
                    break;

                case "allow" or "disallow" when value.Length > 0:
                    if (current is null) break; // grup basligi olmayan kural yok sayilir
                    inHeader = false;
                    current.Value.Directives.Add(new Directive(key == "allow", value, BuildMatcher(value)));
                    break;
            }
        }

        var mine = groups.Where(g => g.Agents.Contains(token)).SelectMany(g => g.Directives).ToList();
        if (mine.Count == 0)
            mine = [.. groups.Where(g => g.Agents.Contains("*")).SelectMany(g => g.Directives)];

        return new RobotsTxt(mine, sitemaps);
    }

    public bool IsAllowed(Uri url) => IsAllowed(url.PathAndQuery);

    public bool IsAllowed(string path)
    {
        if (_directives.Count == 0) return true;

        Directive? best = null;
        foreach (var directive in _directives)
        {
            if (!SafeMatch(directive.Matcher, path)) continue;
            if (best is null
                || directive.Pattern.Length > best.Pattern.Length
                || (directive.Pattern.Length == best.Pattern.Length && directive.Allow))
                best = directive;
        }

        return best is null || best.Allow;
    }

    /// <summary>robots desenini regex'e cevirir: * herhangi bir dizi, sondaki $ tam eslesme.</summary>
    private static Regex BuildMatcher(string pattern)
    {
        var value = pattern;
        var anchored = value.EndsWith('$');
        if (anchored) value = value[..^1];

        var sb = new StringBuilder("^");
        foreach (var ch in value)
            sb.Append(ch == '*' ? ".*" : Regex.Escape(ch.ToString()));
        if (anchored) sb.Append('$');

        return new Regex(sb.ToString(), RegexOptions.CultureInvariant, MatchTimeout);
    }

    private static bool SafeMatch(Regex regex, string path)
    {
        try
        {
            return regex.IsMatch(path);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
