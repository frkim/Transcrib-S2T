using System.Net;
using System.Text;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Transcrib.Functions.Services;
using Xunit;

namespace Transcrib.Functions.Tests;

public class AzureSpeechBatchTranscriptionServiceTests
{
    private const string Endpoint = "https://speech.example.cognitiveservices.azure.com/";
    private const string ResultUrl = "https://storage.example/results/transcription.json?sig=abc";

    private sealed class FakeCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new(GetToken(requestContext, cancellationToken));
    }

    private sealed class FakeSasProvider : IAudioSasUrlProvider
    {
        public string? RequestedBlob { get; private set; }

        public Task<Uri> GetReadUrlAsync(string audioBlobName, CancellationToken cancellationToken = default)
        {
            RequestedBlob = audioBlobName;
            return Task.FromResult(new Uri($"https://storage.example/audio/{audioBlobName}?sig=read"));
        }
    }

    private sealed class FakeSpeechHandler : HttpMessageHandler
    {
        private readonly Queue<string> _statuses;
        private readonly string _resultJson;

        public List<(HttpMethod Method, string Url, string? Auth, string? Body)> Requests { get; } = new();

        public FakeSpeechHandler(IEnumerable<string> statuses, string resultJson)
        {
            _statuses = new Queue<string>(statuses);
            _resultJson = resultJson;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, url, request.Headers.Authorization?.Parameter, body));

            if (url == ResultUrl)
            {
                return Json(_resultJson);
            }

            if (request.Method == HttpMethod.Post)
            {
                return Json($$"""{ "self": "{{Endpoint}}speechtotext/transcriptions/abc-123?api-version=2024-11-15", "status": "NotStarted" }""", HttpStatusCode.Created);
            }

            if (request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (url.Contains("/files"))
            {
                return Json($$"""{ "values": [ { "kind": "TranscriptionReport", "links": { "contentUrl": "https://storage.example/report.json" } }, { "kind": "Transcription", "links": { "contentUrl": "{{ResultUrl}}" } } ] }""");
            }

            var status = _statuses.Count > 1 ? _statuses.Dequeue() : _statuses.Peek();
            return Json($$"""{ "status": "{{status}}", "properties": { "error": { "message": "bad audio" } } }""");
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode code = HttpStatusCode.OK) =>
            new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private const string ResultJson = """
    {
      "recognizedPhrases": [
        { "speaker": 2, "offsetMilliseconds": 61000, "durationMilliseconds": 5000, "nBest": [ { "display": "Avec plaisir" } ] },
        { "speaker": 1, "offsetMilliseconds": 3200, "durationMilliseconds": 4300, "nBest": [ { "display": "Bonjour" } ] }
      ]
    }
    """;

    private static AzureSpeechBatchTranscriptionService CreateService(FakeSpeechHandler handler, FakeSasProvider sas, TimeSpan? timeout = null) =>
        new(Endpoint, new FakeCredential(), "fr-FR", sas, TimeSpan.Zero, timeout ?? TimeSpan.FromSeconds(30),
            NullLogger<AzureSpeechBatchTranscriptionService>.Instance, new HttpClient(handler));

    [Fact]
    public async Task TranscribeBlobAsync_Succeeds_SubmitsPollsFetchesAndCleansUp()
    {
        var handler = new FakeSpeechHandler(["NotStarted", "Running", "Succeeded"], ResultJson);
        var sas = new FakeSasProvider();
        var service = CreateService(handler, sas);

        var result = await service.TranscribeBlobAsync("job1.mp3", Stream.Null, "meeting.mp3");

        var lines = result.Text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        Assert.Equal("[00:00:03 → 00:00:07] Speaker 1: Bonjour", lines[0]);
        Assert.Equal("[00:01:01 → 00:01:06] Speaker 2: Avec plaisir", lines[1]);
        Assert.Equal("job1.mp3", sas.RequestedBlob);

        var submit = handler.Requests.First(r => r.Method == HttpMethod.Post);
        Assert.Equal($"{Endpoint}speechtotext/transcriptions?api-version=2024-11-15", submit.Url);
        Assert.Equal("token", submit.Auth);
        Assert.Contains("https://storage.example/audio/job1.mp3?sig=read", submit.Body);
        Assert.Contains("\"locale\":\"fr-FR\"", submit.Body);
        Assert.Contains("\"enabled\":true", submit.Body);

        // The SAS-protected result file must be fetched without the Entra bearer token.
        Assert.Null(handler.Requests.Single(r => r.Url == ResultUrl).Auth);

        Assert.Equal(HttpMethod.Delete, handler.Requests[^1].Method);
        Assert.EndsWith("/transcriptions/abc-123?api-version=2024-11-15", handler.Requests[^1].Url);
    }

    [Fact]
    public async Task TranscribeBlobAsync_ServiceReportsFailure_ThrowsAndCleansUp()
    {
        var handler = new FakeSpeechHandler(["Running", "Failed"], ResultJson);
        var service = CreateService(handler, new FakeSasProvider());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TranscribeBlobAsync("job1.mp3", Stream.Null, "meeting.mp3"));

        Assert.Contains("bad audio", ex.Message);
        Assert.Equal(HttpMethod.Delete, handler.Requests[^1].Method);
    }

    [Fact]
    public async Task TranscribeBlobAsync_NeverCompletes_TimesOut()
    {
        var handler = new FakeSpeechHandler(["Running"], ResultJson);
        var service = CreateService(handler, new FakeSasProvider(), TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAsync<TimeoutException>(() =>
            service.TranscribeBlobAsync("job1.mp3", Stream.Null, "meeting.mp3"));

        Assert.Equal(HttpMethod.Delete, handler.Requests[^1].Method);
    }

    [Fact]
    public void BuildTranscript_LegacyTicksAndMissingSpeaker_AreHandled()
    {
        // 32,000,000 ticks = 3.2 s; 43,000,000 ticks = 4.3 s
        var payload = """
        { "recognizedPhrases": [ { "offsetInTicks": 32000000, "durationInTicks": 43000000, "nBest": [ { "display": "Salut" } ] } ] }
        """;

        Assert.Equal("[00:00:03 → 00:00:07] Speaker Unknown: Salut", AzureSpeechBatchTranscriptionService.BuildTranscript(payload));
    }

    [Fact]
    public void BuildTranscript_NoPhrases_FallsBackToCombinedText()
    {
        var payload = """{ "combinedRecognizedPhrases": [ { "display": "Texte complet." } ] }""";

        Assert.Equal("Texte complet.", AzureSpeechBatchTranscriptionService.BuildTranscript(payload));
    }

    [Fact]
    public async Task TranscribeAsync_StreamOverload_IsNotSupported()
    {
        var service = CreateService(new FakeSpeechHandler(["Succeeded"], ResultJson), new FakeSasProvider());

        await Assert.ThrowsAsync<NotSupportedException>(() => service.TranscribeAsync(Stream.Null, "x.mp3"));
    }
}
