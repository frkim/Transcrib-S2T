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
builder.Services.AddSingleton<ITranscriptionService>(sp =>
{
    var endpoint = configuration["Speech__Endpoint"] ?? configuration["Speech:Endpoint"]
        ?? throw new InvalidOperationException("Speech endpoint is not configured.");
    var language = configuration["Speech__Language"] ?? configuration["Speech:Language"] ?? "fr-FR";
    var logger = sp.GetRequiredService<ILogger<AzureSpeechTranscriptionService>>();
    return new AzureSpeechTranscriptionService(endpoint, credential, language, logger);
});

builder.Services.AddSingleton<TranscriptionProcessor>();

builder.Build().Run();
