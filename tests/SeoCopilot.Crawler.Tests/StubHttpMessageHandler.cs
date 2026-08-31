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

    public List<string> Requests { get; } = [];

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

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var url = request.RequestUri!.AbsoluteUri;
        Requests.Add(url);

        var response = _routes.TryGetValue(url, out var factory)
            ? factory()
            : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(string.Empty) };

        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
