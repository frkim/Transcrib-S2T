using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Transcrib.Functions.Services;

/// <summary>
/// Azure AI Speech implementation of <see cref="ITranscriptionService"/> using the
/// synchronous Fast Transcription REST API. The service decodes compressed audio
/// (MP3, etc.) server-side, so no local GStreamer/codec is required on the host.
/// Diarization is enabled so each phrase is attributed to a speaker.
/// Authentication uses Microsoft Entra ID (Managed Identity) — no subscription key.
/// </summary>
public class AzureSpeechTranscriptionService : ITranscriptionService
{
    private const string ApiVersion = "2024-11-15";
    private const int MaxSpeakers = 10;

    // Entra ID scope for Azure AI / Cognitive Services data-plane access.
    private static readonly string[] Scopes = ["https://cognitiveservices.azure.com/.default"];

    private readonly string _endpoint;
    private readonly TokenCredential _credential;
    private readonly string _language;
    private readonly ILogger<AzureSpeechTranscriptionService> _logger;
    private readonly HttpClient _httpClient;

    public AzureSpeechTranscriptionService(string endpoint, TokenCredential credential, string language, ILogger<AzureSpeechTranscriptionService> logger)
    {
        _endpoint = endpoint;
        _credential = credential;
        _language = language;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken = default)
    {
        // Buffer the audio so it can be sent as a multipart form part.
        using var buffer = new MemoryStream();
        await audio.CopyToAsync(buffer, cancellationToken);
        var audioBytes = buffer.ToArray();

        var requestUri = $"{_endpoint.TrimEnd('/')}/speechtotext/transcriptions:transcribe?api-version={ApiVersion}";

        var definition = JsonSerializer.Serialize(new
        {
            locales = new[] { _language },
            diarization = new { maxSpeakers = MaxSpeakers, enabled = true },
        });

        using var form = new MultipartFormDataContent();

        var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        form.Add(audioContent, "audio", string.IsNullOrWhiteSpace(fileName) ? "audio.mp3" : fileName);

        var definitionContent = new StringContent(definition, Encoding.UTF8);
        form.Add(definitionContent, "definition");

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = form };

        var token = await _credential.GetTokenAsync(new TokenRequestContext(Scopes), cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Fast transcription failed: {Status} {Body}", (int)response.StatusCode, payload);
            throw new InvalidOperationException($"Fast transcription returned {(int)response.StatusCode}: {payload}");
        }

        return new TranscriptionResult(BuildTranscript(payload));
    }

    /// <summary>
    /// Builds a human-readable transcript from the Fast Transcription response,
    /// attributing each phrase to its diarized speaker and prefixing it with the
    /// <c>[start → end]</c> time range (extracted from the phrase offset/duration).
    /// </summary>
    internal static string BuildTranscript(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var transcript = new StringBuilder();

        if (root.TryGetProperty("phrases", out var phrases) && phrases.ValueKind == JsonValueKind.Array && phrases.GetArrayLength() > 0)
        {
            foreach (var phrase in phrases.EnumerateArray())
            {
                var text = phrase.TryGetProperty("text", out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var speaker = phrase.TryGetProperty("speaker", out var s) && s.ValueKind == JsonValueKind.Number
                    ? s.GetInt32().ToString()
                    : "Unknown";

                var offset = phrase.TryGetProperty("offsetMilliseconds", out var o) && o.ValueKind == JsonValueKind.Number
                    ? o.GetInt64()
                    : 0L;
                var duration = phrase.TryGetProperty("durationMilliseconds", out var d) && d.ValueKind == JsonValueKind.Number
                    ? d.GetInt64()
                    : 0L;

                var start = FormatTimestamp(offset);
                var end = FormatTimestamp(offset + duration);

                transcript.AppendLine($"[{start} → {end}] Speaker {speaker}: {text}");
            }
        }
        else if (root.TryGetProperty("combinedPhrases", out var combined) && combined.ValueKind == JsonValueKind.Array)
        {
            // Fallback: no per-speaker phrases, use the combined text.
            foreach (var item in combined.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var t) && !string.IsNullOrWhiteSpace(t.GetString()))
                {
                    transcript.AppendLine(t.GetString());
                }
            }
        }

        return transcript.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats a millisecond offset as an <c>hh:mm:ss</c> timestamp.
    /// </summary>
    private static string FormatTimestamp(long milliseconds) =>
        TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss");
}
