using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SeoCopilot.Infrastructure.Clients;
using SkiaSharp;

namespace SeoCopilot.Api.Tests;

/// <summary>
/// Cloudflare Workers AI istemcisi sahte HTTP isleyicisiyle: istek bicimi, yanit cozumu ve
/// hata durumlarinda null donmesi. Disariya istek atmaz.
/// </summary>
public class CloudflareImageClientTests
{
    private const string Model = "@cf/black-forest-labs/flux-2-klein-4b";

    /// <summary>Istegi kaydeder (govde istemci donmeden okunur) ve hazir yaniti doner.</summary>
    private sealed class RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? Request { get; private set; }
        public Dictionary<string, string> Form { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Request = request;

            if (request.Content is MultipartFormDataContent multipart)
            {
                foreach (var part in multipart)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"') ?? string.Empty;
                    Form[name] = await part.ReadAsStringAsync(cancellationToken);
                }
            }

            return await respond(request, cancellationToken);
        }
    }

    private static (CloudflareImageClient Client, RecordingHandler Handler) Create(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond,
        string accountId = "hesap-1", string token = "token-1", int timeoutSeconds = 30,
        int maxFlaggedRetries = 2)
    {
        var handler = new RecordingHandler(respond);
        var options = Options.Create(new CloudflareAiOptions
        {
            AccountId = accountId,
            ApiToken = token,
            BaseUrl = "https://cf.test/client/v4/",
            Model = Model,
            TimeoutSeconds = timeoutSeconds,
            MaxFlaggedRetries = maxFlaggedRetries
        });

        var client = new CloudflareImageClient(
            new HttpClient(handler), options, NullLogger<CloudflareImageClient>.Instance);
        return (client, handler);
    }

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond(
        HttpStatusCode status, string body) =>
        (_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });

    private static string Envelope(byte[] image) =>
        $$"""{"result":{"image":"{{Convert.ToBase64String(image)}}"},"success":true,"errors":[],"messages":[]}""";

    private static byte[] Image(int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(new SKColor(40, 90, 130));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    [Theory]
    [InlineData("16:9", 1024, 576)]
    [InlineData("1:1", 1024, 1024)]
    [InlineData("9:16", 576, 1024)]
    [InlineData("tanimsiz", 1024, 1024)]
    public async Task Sends_the_prompt_and_platform_size_as_multipart(string aspect, int width, int height)
    {
        var (client, handler) = Create(Respond(HttpStatusCode.OK, Envelope(Image(width, height))));

        var image = await client.GenerateAsync("factory floor at dawn, no text", aspect);

        Assert.NotNull(image);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal($"/client/v4/accounts/hesap-1/ai/run/{Model}", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization?.Scheme);
        Assert.Equal("token-1", handler.Request.Headers.Authorization?.Parameter);

        Assert.Equal("factory floor at dawn, no text", handler.Form["prompt"]);
        Assert.Equal(width.ToString(), handler.Form["width"]);
        Assert.Equal(height.ToString(), handler.Form["height"]);
    }

    [Fact]
    public async Task Decodes_the_image_and_reads_its_real_size_and_format()
    {
        // Model istenen olcuyu yuvarlayabilir — kayda yanittaki gercek olcu yazilmali.
        var png = Image(1008, 560, SKEncodedImageFormat.Png);
        var (client, _) = Create(Respond(HttpStatusCode.OK, Envelope(png)));

        var image = await client.GenerateAsync("brief", "16:9");

        Assert.NotNull(image);
        Assert.Equal(png, image.Content);
        Assert.Equal("image/png", image.ContentType);
        Assert.Equal((1008, 560), (image.Width, image.Height));
        Assert.Equal(Model, image.Model);
    }

    [Fact]
    public async Task Response_without_the_envelope_is_accepted()
    {
        var jpeg = Image(1024, 1024);
        var (client, _) = Create(Respond(HttpStatusCode.OK,
            $$"""{"image":"{{Convert.ToBase64String(jpeg)}}"}"""));

        var image = await client.GenerateAsync("brief", "1:1");

        Assert.NotNull(image);
        Assert.Equal("image/jpeg", image.ContentType);
    }

    [Fact]
    public async Task Long_prompts_are_clipped()
    {
        var (client, handler) = Create(Respond(HttpStatusCode.OK, Envelope(Image(1024, 1024))));

        await client.GenerateAsync(new string('a', 5000), "1:1");

        Assert.Equal(CloudflareImageClient.MaxPromptLength, handler.Form["prompt"].Length);
    }

    public static TheoryData<HttpStatusCode, string> Failures => new()
    {
        // Gunluk ucretsiz kota ya da hiz siniri
        { HttpStatusCode.TooManyRequests, """{"success":false,"errors":[{"code":3036,"message":"daily free allocation exceeded"}]}""" },
        { HttpStatusCode.Unauthorized, """{"success":false,"errors":[{"code":10000,"message":"Authentication error"}]}""" },
        { HttpStatusCode.OK, """{"result":null,"success":false,"errors":[{"code":5006,"message":"bad input"}]}""" },
        { HttpStatusCode.OK, """{"result":{},"success":true}""" },
        { HttpStatusCode.OK, """{"result":{"image":"base64-degil!!"},"success":true}""" },
        // Gecerli base64 ama gorsel degil
        { HttpStatusCode.OK, """{"result":{"image":"bWVyaGFiYQ=="},"success":true}""" },
        { HttpStatusCode.OK, "<html>bad gateway</html>" }
    };

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task Failures_return_null_instead_of_throwing(HttpStatusCode status, string body)
    {
        var (client, handler) = Create(Respond(status, body));

        Assert.Null(await client.GenerateAsync("brief", "1:1"));
        // Kota, yetki ve bicim hatalari tekrarla duzelmez — yeniden denenmez.
        Assert.Equal(1, handler.Calls);
    }

    /// <summary>Canli testte gorulen yanit (zararsiz bir TV tamiri istemi).</summary>
    private const string Flagged =
        """{"errors":[{"message":"AiError: AiError: Your output has been flagged. Please choose another prompt / input image combination (e900ad1a)","code":3030}],"success":false,"result":{},"messages":[]}""";

    [Fact]
    public async Task Flagged_output_is_retried_until_an_image_arrives()
    {
        var calls = 0;
        var (client, handler) = Create((_, _) => Task.FromResult(++calls < 3
            ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(Flagged) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Envelope(Image(1024, 1024))) }));

        var image = await client.GenerateAsync("tv repair workshop", "1:1");

        Assert.NotNull(image);
        Assert.Equal(3, handler.Calls);
        // Her deneme tam istek gonderir — govde ilk gonderimde tuketilmis olsa da.
        Assert.Equal("tv repair workshop", handler.Form["prompt"]);
        Assert.Equal("1024", handler.Form["width"]);
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(0, 1)]
    public async Task Retries_stop_at_the_limit(int maxRetries, int expectedCalls)
    {
        var (client, handler) = Create(
            Respond(HttpStatusCode.BadRequest, Flagged), maxFlaggedRetries: maxRetries);

        Assert.Null(await client.GenerateAsync("brief", "1:1"));
        Assert.Equal(expectedCalls, handler.Calls);
    }

    [Fact]
    public async Task Timeout_returns_null()
    {
        var (client, _) = Create(
            async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            timeoutSeconds: 1);

        Assert.Null(await client.GenerateAsync("brief", "1:1"));
    }

    [Theory]
    [InlineData("", "token-1")]
    [InlineData("hesap-1", "")]
    public async Task Missing_credentials_disable_the_client_without_calling_the_api(string accountId, string token)
    {
        var (client, handler) = Create(Respond(HttpStatusCode.OK, "{}"), accountId, token);

        Assert.False(client.IsEnabled);
        Assert.Null(await client.GenerateAsync("brief", "1:1"));
        Assert.Equal(0, handler.Calls);
    }
}
