namespace Transcrib.Functions.Services;

public record TranscriptionResult(string Text);

/// <summary>
/// Abstraction over the speech-to-text engine. Implemented by
/// <see cref="AzureSpeechTranscriptionService"/> using Azure AI Speech with
/// speaker diarization enabled (Fast Transcription), or
/// <see cref="AzureSpeechBatchTranscriptionService"/> (Batch Transcription).
/// Abstracted for unit testing.
/// </summary>
public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes the audio stored in the <c>audio</c> container under <paramref name="audioBlobName"/>.
    /// Defaults to the stream-based <see cref="TranscribeAsync"/>; implementations that need a
    /// blob URL instead of the audio bytes (Batch Transcription) override it.
    /// </summary>
    Task<TranscriptionResult> TranscribeBlobAsync(string audioBlobName, Stream audio, string fileName, CancellationToken cancellationToken = default) =>
        TranscribeAsync(audio, fileName, cancellationToken);
}
