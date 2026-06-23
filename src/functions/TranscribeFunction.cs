using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Transcrib.Functions.Processing;

namespace Transcrib.Functions;

/// <summary>
/// Pro-code transcription pipeline. Triggered automatically when an MP3 is added
/// to the <c>audio</c> container; delegates to <see cref="TranscriptionProcessor"/>.
/// </summary>
public class TranscribeFunction
{
    private readonly TranscriptionProcessor _processor;
    private readonly ILogger<TranscribeFunction> _logger;

    public TranscribeFunction(TranscriptionProcessor processor, ILogger<TranscribeFunction> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [Function(nameof(TranscribeFunction))]
    public async Task Run(
        [BlobTrigger("audio/{name}", Source = BlobTriggerSource.EventGrid, Connection = "AudioStorage")] Stream audio,
        string name,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transcription triggered for blob {Name}", name);
        await _processor.ProcessAsync(name, audio, cancellationToken);
    }
}
