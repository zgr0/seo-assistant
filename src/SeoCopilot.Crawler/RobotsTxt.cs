namespace SeoCopilot.Crawler;

/// <summary>Minimal robots.txt ayristirici — User-agent: * bloguna gore Allow/Disallow.</summary>
public sealed class RobotsTxt
{
    private readonly List<string> _disallow = [];
    private readonly List<string> _allow = [];

    public IReadOnlyList<string> Sitemaps { get; } = new List<string>();

    public static RobotsTxt Parse(string content)
    {
        var robots = new RobotsTxt();
        var sitemaps = (List<string>)robots.Sitemaps;
        var appliesToUs = false;

        foreach (var raw in content.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var idx = line.IndexOf(':');
            if (idx < 0) continue;
            var key = line[..idx].Trim().ToLowerInvariant();
            var val = line[(idx + 1)..].Trim();

            switch (key)
            {
                case "user-agent":
                    appliesToUs = val is "*";
                    break;
                case "disallow" when appliesToUs && val.Length > 0:
                    robots._disallow.Add(val);
                    break;
                case "allow" when appliesToUs && val.Length > 0:
                    robots._allow.Add(val);
                    break;
                case "sitemap":
                    sitemaps.Add(val);
                    break;
            }
        }

        return robots;
    }

    public bool IsAllowed(string path)
    {
        if (_allow.Any(path.StartsWith)) return true;
        return !_disallow.Any(path.StartsWith);
    }
}
