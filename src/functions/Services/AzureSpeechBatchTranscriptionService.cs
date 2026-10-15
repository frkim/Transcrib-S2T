using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Transcrib.Functions.Services;

/// <summary>
/// Azure AI Speech implementation of <see cref="ITranscriptionService"/> using the asynchronous
/// Batch Transcription REST API (submit → poll → fetch result). It is cheaper than Fast
/// Transcription and suited to large, non-urgent volumes. Audio is read by the service from a
/// short-lived read-only SAS URL (see <see cref="IAudioSasUrlProvider"/>), diarization is enabled
/// and authentication uses Microsoft Entra ID (Managed Identity) — no subscription key.
/// </summary>
public class AzureSpeechBatchTranscriptionService : ITranscriptionService
{
    private const string ApiVersion = "2024-11-15";
    private const int MaxSpeakers = 10;

    private static readonly string[] Scopes = ["https://cognitiveservices.azure.com/.default"];

    private readonly string _endpoint;
    private readonly TokenCredential _credential;
    private readonly string _language;
    private readonly IAudioSasUrlProvider _audioUrls;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _timeout;
    private readonly ILogger<AzureSpeechBatchTranscriptionService> _logger;
    private readonly HttpClient _httpClient;

    public AzureSpeechBatchTranscriptionService(
        string endpoint,
        TokenCredential credential,
        string language,
        IAudioSasUrlProvider audioUrls,
        TimeSpan pollInterval,
        TimeSpan timeout,
        ILogger<AzureSpeechBatchTranscriptionService> logger,
        HttpClient? httpClient = null)
    {
        _endpoint = endpoint;
        _credential = credential;
        _language = language;
        _audioUrls = audioUrls;
        _pollInterval = pollInterval;
        _timeout = timeout;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Batch Transcription reads the audio from its blob URL; use TranscribeBlobAsync.");

    public async Task<TranscriptionResult> TranscribeBlobAsync(string audioBlobName, Stream audio, string fileName, CancellationToken cancellationToken = default)
    {
        var contentUrl = await _audioUrls.GetReadUrlAsync(audioBlobName, cancellationToken);
        var transcriptionId = await SubmitAsync(contentUrl, fileName, audioBlobName, cancellationToken);
        _logger.LogInformation("Batch transcription {TranscriptionId} submitted for blob {BlobName}", transcriptionId, audioBlobName);

        try
        {
            await WaitForCompletionAsync(transcriptionId, cancellationToken);
            var resultUrl = await GetResultUrlAsync(transcriptionId, cancellationToken);

            // The result file is served from a SAS URL: it must not receive the Entra bearer token.
            using var resultResponse = await _httpClient.GetAsync(resultUrl, cancellationToken);
            var payload = await resultResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!resultResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Batch transcription result download returned {(int)resultResponse.StatusCode}.");
            }

            return new TranscriptionResult(BuildTranscript(payload));
        }
        finally
        {
            await DeleteQuietlyAsync(transcriptionId);
        }
    }

    private Uri TranscriptionUri(string? transcriptionId = null, string? suffix = null)
    {
        var path = $"{_endpoint.TrimEnd('/')}/speechtotext/transcriptions";
        if (transcriptionId is not null)
        {
            path += $"/{Uri.EscapeDataString(transcriptionId)}";
        }

        return new Uri($"{path}{suffix}?api-version={ApiVersion}");
    }

    private async Task<string> SubmitAsync(Uri contentUrl, string fileName, string audioBlobName, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            displayName = string.IsNullOrWhiteSpace(fileName) ? audioBlobName : fileName,
            locale = _language,
            contentUrls = new[] { contentUrl.ToString() },
            properties = new
            {
                diarization = new { maxSpeakers = MaxSpeakers, enabled = true },
            },
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, TranscriptionUri())
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        using var response = await SendAuthorizedAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Batch transcription submission failed: {Status} {Body}", (int)response.StatusCode, payload);
            throw new InvalidOperationException($"Batch transcription submission returned {(int)response.StatusCode}: {payload}");
        }

        using var doc = JsonDocument.Parse(payload);
        var self = doc.RootElement.TryGetProperty("self", out var s) ? s.GetString() : null;
        self ??= response.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(self))
        {
            throw new InvalidOperationException("Batch transcription submission did not return a transcription URL.");
        }

        // .../transcriptions/{id}?api-version=...
        return Uri.UnescapeDataString(new Uri(self).Segments[^1].TrimEnd('/'));
    }

    private async Task WaitForCompletionAsync(string transcriptionId, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            while (true)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, TranscriptionUri(transcriptionId));
                using var response = await SendAuthorizedAsync(request, timeoutCts.Token);
                var payload = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Batch transcription status returned {(int)response.StatusCode}: {payload}");
                }

                using var doc = JsonDocument.Parse(payload);
                var status = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : null;
                switch (status)
                {
                    case "Succeeded":
                        return;
                    case "Failed":
                        var message = doc.RootElement.TryGetProperty("properties", out var props)
                            && props.TryGetProperty("error", out var err)
                            && err.TryGetProperty("message", out var m)
                                ? m.GetString()
                                : null;
                        throw new InvalidOperationException($"Batch transcription failed: {message ?? "unknown error"}");
                }

                await Task.Delay(_pollInterval, timeoutCts.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Batch transcription {transcriptionId} did not complete within {_timeout}.");
        }
    }

    private async Task<Uri> GetResultUrlAsync(string transcriptionId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, TranscriptionUri(transcriptionId, "/files"));
        using var response = await SendAuthorizedAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Batch transcription files returned {(int)response.StatusCode}: {payload}");
        }

        using var doc = JsonDocument.Parse(payload);
        if (doc.RootElement.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array)
        {
            foreach (var file in values.EnumerateArray())
            {
                if (file.TryGetProperty("kind", out var kind) && kind.GetString() == "Transcription"
                    && file.TryGetProperty("links", out var links)
                    && links.TryGetProperty("contentUrl", out var url)
                    && Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri))
                {
                    return uri;
                }
            }
        }

        throw new InvalidOperationException("Batch transcription completed without a transcription result file.");
    }

    private async Task DeleteQuietlyAsync(string transcriptionId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, TranscriptionUri(transcriptionId));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var response = await SendAuthorizedAsync(request, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete batch transcription {TranscriptionId}", transcriptionId);
        }
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext(Scopes), cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Builds the same human-readable transcript as Fast Transcription
    /// (<c>[start → end] Speaker N: text</c>) from a Batch Transcription result file.
    /// </summary>
    internal static string BuildTranscript(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var transcript = new StringBuilder();

        if (root.TryGetProperty("recognizedPhrases", out var phrases) && phrases.ValueKind == JsonValueKind.Array && phrases.GetArrayLength() > 0)
        {
            var lines = new List<(long Offset, string Line)>();
            foreach (var phrase in phrases.EnumerateArray())
            {
                var text = FirstDisplayText(phrase);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var speaker = phrase.TryGetProperty("speaker", out var s) && s.ValueKind == JsonValueKind.Number
                    ? s.GetInt32().ToString()
                    : "Unknown";
                var offset = ReadMilliseconds(phrase, "offsetMilliseconds", "offsetInTicks");
                var duration = ReadMilliseconds(phrase, "durationMilliseconds", "durationInTicks");

                lines.Add((offset, AzureSpeechTranscriptionService.FormatLine(offset, duration, speaker, text)));
            }

            // Chronological order (phrases are grouped per channel in the raw result).
            foreach (var (_, line) in lines.OrderBy(l => l.Offset))
            {
                transcript.AppendLine(line);
            }
        }
        else if (root.TryGetProperty("combinedRecognizedPhrases", out var combined) && combined.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in combined.EnumerateArray())
            {
                if (item.TryGetProperty("display", out var d) && !string.IsNullOrWhiteSpace(d.GetString()))
                {
                    transcript.AppendLine(d.GetString());
                }
            }
        }

        return transcript.ToString().TrimEnd();
    }

    private static string? FirstDisplayText(JsonElement phrase)
    {
        if (phrase.TryGetProperty("nBest", out var nBest) && nBest.ValueKind == JsonValueKind.Array && nBest.GetArrayLength() > 0
            && nBest[0].TryGetProperty("display", out var display))
        {
            return display.GetString();
        }

        return null;
    }

    private static long ReadMilliseconds(JsonElement phrase, string millisecondsProperty, string ticksProperty)
    {
        if (phrase.TryGetProperty(millisecondsProperty, out var ms) && ms.ValueKind == JsonValueKind.Number)
        {
            return ms.GetInt64();
        }

        // Legacy result format expresses offsets in 100-ns ticks.
        return phrase.TryGetProperty(ticksProperty, out var ticks) && ticks.ValueKind == JsonValueKind.Number
            ? ticks.GetInt64() / 10_000
            : 0L;
    }
}
