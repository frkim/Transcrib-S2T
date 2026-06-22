namespace Transcrib.Functions.Services;

public record TranscriptionResult(string Text);

/// <summary>
/// Abstraction over the speech-to-text engine. Implemented by
/// <see cref="AzureSpeechTranscriptionService"/> using Azure AI Speech with
/// speaker diarization enabled. Abstracted for unit testing.
/// </summary>
public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken = default);
}
