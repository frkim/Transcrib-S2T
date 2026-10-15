using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Transcrib.Functions.Processing;
using Transcrib.Functions.Services;
using Transcrib.Shared.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var credential = new DefaultAzureCredential();
var configuration = builder.Configuration;

// Cosmos DB — shared jobs container.
builder.Services.AddSingleton(_ =>
{
    var endpoint = configuration["Cosmos__Endpoint"] ?? configuration["Cosmos:Endpoint"]
        ?? throw new InvalidOperationException("Cosmos endpoint is not configured.");
    var options = new CosmosClientOptions { Serializer = new SystemTextJsonCosmosSerializer() };
    return new CosmosClient(endpoint, credential, options);
});
builder.Services.AddSingleton<IJobRepository>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var database = configuration["Cosmos__Database"] ?? configuration["Cosmos:Database"] ?? "transcrib";
    var container = configuration["Cosmos__Container"] ?? configuration["Cosmos:Container"] ?? "jobs";
    return new CosmosJobRepository(client.GetContainer(database, container));
});

// Blob Storage — transcripts output.
builder.Services.AddSingleton(_ =>
{
    var serviceUri = configuration["Storage__BlobEndpoint"] ?? configuration["Storage:BlobEndpoint"]
        ?? throw new InvalidOperationException("Blob endpoint is not configured.");
    return new BlobServiceClient(new Uri(serviceUri), credential);
});
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

// Azure AI Speech (diarization enabled) — authenticated via Managed Identity.
// Speech__Mode selects the engine: "Fast" (default, synchronous Fast Transcription) or
// "Batch" (asynchronous Batch Transcription, cheaper for large non-urgent volumes).
builder.Services.AddSingleton<ITranscriptionService>(sp =>
{
    var endpoint = configuration["Speech__Endpoint"] ?? configuration["Speech:Endpoint"]
        ?? throw new InvalidOperationException("Speech endpoint is not configured.");
    var language = configuration["Speech__Language"] ?? configuration["Speech:Language"] ?? "fr-FR";
    var mode = configuration["Speech__Mode"] ?? configuration["Speech:Mode"] ?? "Fast";

    if (string.Equals(mode, "Batch", StringComparison.OrdinalIgnoreCase))
    {
        var pollSeconds = int.TryParse(configuration["Speech__BatchPollingSeconds"] ?? configuration["Speech:BatchPollingSeconds"], out var p) && p > 0 ? p : 15;
        var timeoutMinutes = int.TryParse(configuration["Speech__BatchTimeoutMinutes"] ?? configuration["Speech:BatchTimeoutMinutes"], out var t) && t > 0 ? t : 60;
        var timeout = TimeSpan.FromMinutes(timeoutMinutes);

        // The SAS must outlive the whole batch job (queue time included).
        var audioUrls = new BlobAudioSasUrlProvider(sp.GetRequiredService<BlobServiceClient>(), timeout + TimeSpan.FromHours(1));
        return new AzureSpeechBatchTranscriptionService(
            endpoint, credential, language, audioUrls, TimeSpan.FromSeconds(pollSeconds), timeout,
            sp.GetRequiredService<ILogger<AzureSpeechBatchTranscriptionService>>());
    }

    if (!string.Equals(mode, "Fast", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Unsupported Speech mode '{mode}'. Expected 'Fast' or 'Batch'.");
    }

    var logger = sp.GetRequiredService<ILogger<AzureSpeechTranscriptionService>>();
    return new AzureSpeechTranscriptionService(endpoint, credential, language, logger);
});

builder.Services.AddSingleton<TranscriptionProcessor>();

builder.Build().Run();
