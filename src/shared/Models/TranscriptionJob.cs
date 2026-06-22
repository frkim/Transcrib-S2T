using System.Text.Json.Serialization;

namespace Transcrib.Shared.Models;

/// <summary>
/// Cosmos DB job document. This schema is the shared contract between the
/// backend API, the Azure Functions pipeline and the Logic Apps workflows.
/// </summary>
public class TranscriptionJob
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("audioBlobUrl")]
    public string AudioBlobUrl { get; set; } = string.Empty;

    [JsonPropertyName("transcriptBlobUrl")]
    public string? TranscriptBlobUrl { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = JobStatus.Processing;

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
