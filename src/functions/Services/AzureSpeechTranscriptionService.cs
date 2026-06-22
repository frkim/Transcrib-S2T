using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Logging;

namespace Transcrib.Functions.Services;

/// <summary>
/// Azure AI Speech implementation of <see cref="ITranscriptionService"/>.
/// Uses <see cref="ConversationTranscriber"/> so speaker diarization is enabled
/// and reflected in the transcript (each line prefixed by its speaker id).
/// </summary>
public class AzureSpeechTranscriptionService : ITranscriptionService
{
    private readonly string _endpoint;
    private readonly string _key;
    private readonly string _language;
    private readonly ILogger<AzureSpeechTranscriptionService> _logger;

    public AzureSpeechTranscriptionService(string endpoint, string key, string language, ILogger<AzureSpeechTranscriptionService> logger)
    {
        _endpoint = endpoint;
        _key = key;
        _language = language;
        _logger = logger;
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken = default)
    {
        var speechConfig = SpeechConfig.FromEndpoint(new Uri(_endpoint), _key);
        speechConfig.SpeechRecognitionLanguage = _language;

        // MP3 compressed input (decoded via GStreamer on the Functions host).
        using var pushStream = AudioInputStream.CreatePushStream(
            AudioStreamFormat.GetCompressedFormat(AudioStreamContainerFormat.MP3));
        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        using var transcriber = new ConversationTranscriber(speechConfig, audioConfig);

        var transcript = new StringBuilder();
        var stop = new TaskCompletionSource<bool>();

        transcriber.Transcribed += (_, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
            {
                var speaker = string.IsNullOrEmpty(e.Result.SpeakerId) ? "Unknown" : e.Result.SpeakerId;
                transcript.AppendLine($"Speaker {speaker}: {e.Result.Text}");
            }
        };
        transcriber.Canceled += (_, e) =>
        {
            if (e.Reason == CancellationReason.Error)
            {
                _logger.LogError("Speech transcription error: {Code} {Details}", e.ErrorCode, e.ErrorDetails);
            }
            stop.TrySetResult(true);
        };
        transcriber.SessionStopped += (_, _) => stop.TrySetResult(true);

        // Feed the MP3 bytes into the push stream.
        var buffer = new byte[4096];
        int read;
        while ((read = await audio.ReadAsync(buffer, cancellationToken)) > 0)
        {
            pushStream.Write(buffer, read);
        }
        pushStream.Close();

        await transcriber.StartTranscribingAsync().ConfigureAwait(false);
        await using (cancellationToken.Register(() => stop.TrySetCanceled()))
        {
            await stop.Task.ConfigureAwait(false);
        }
        await transcriber.StopTranscribingAsync().ConfigureAwait(false);

        return new TranscriptionResult(transcript.ToString().TrimEnd());
    }
}
