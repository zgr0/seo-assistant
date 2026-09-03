using System.Net;
using System.Text;

namespace SeoCopilot.Crawler.Tests;

/// <summary>
/// Ag'a cikmadan HttpClient beslemek icin elle yazilmis handler — projede mock kutuphanesi yok.
/// Yanitlar mutlak URL'e gore eslesir.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<HttpMethod, HttpResponseMessage>> _methodRoutes = new(StringComparer.OrdinalIgnoreCase);

    private readonly Lock _calls = new();

    public List<string> Requests { get; } = [];

    /// <summary>Metodu da tasiyan cagri kaydi — HEAD/GET ayrimini dogrulamak icin.</summary>
    public List<(string Method, string Url)> Calls { get; } = [];

    public StubHttpMessageHandler Map(string url, string body, string contentType = "text/html", HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes[url] = () =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, contentType)
            };
            return response;
        };
        return this;
    }

    /// <summary>
    /// Govdeyi oldugu gibi, verilen Content-Type ile servis eder. <see cref="Map"/> her zaman
    /// UTF-8 kodladigi icin UTF-8 disi charset'leri denemek buradan gecer.
    /// </summary>
    public StubHttpMessageHandler MapBytes(string url, byte[] body, string contentType)
    {
        _routes[url] = () =>
        {
            var content = new ByteArrayContent(body);
            content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        };
        return this;
    }

    public StubHttpMessageHandler MapRedirect(string url, string location, HttpStatusCode status = HttpStatusCode.MovedPermanently)
    {
        _routes[url] = () =>
        {
            var response = new HttpResponseMessage(status);
            response.Headers.Location = new Uri(location);
            return response;
        };
        return this;
    }

    public StubHttpMessageHandler MapHeader(string url, string body, string headerName, string headerValue)
    {
        _routes[url] = () =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/html")
            };
            response.Headers.TryAddWithoutValidation(headerName, headerValue);
            return response;
        };
        return this;
    }

    /// <summary>HEAD'e 405 donen sunucular icin — GET normal yanit verir.</summary>
    public StubHttpMessageHandler MapHeadNotAllowed(string url, string body, string contentType)
    {
        _methodRoutes[url] = method => method == HttpMethod.Head
            ? new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, contentType)
            };
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var url = request.RequestUri!.AbsoluteUri;

        // Gorsel olcumu istekleri ust uste binebilir — kayit listeleri korunmali.
        lock (_calls)
        {
            Requests.Add(url);
            Calls.Add((request.Method.Method, url));
        }

        HttpResponseMessage response;
        if (_methodRoutes.TryGetValue(url, out var byMethod))
            response = byMethod(request.Method);
        else
            response = _routes.TryGetValue(url, out var factory)
                ? factory()
                : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(string.Empty) };

        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
