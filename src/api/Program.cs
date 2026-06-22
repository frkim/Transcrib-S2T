using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using Transcrib.Api.Services;
using Transcrib.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Observability — Application Insights (no-op when connection string absent).
builder.Services.AddApplicationInsightsTelemetry();

// Entra ID authentication. Enabled only when an AzureAd section is configured,
// so the API stays runnable locally without an app registration.
var azureAdConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"]);
if (azureAdConfigured)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}
builder.Services.AddAuthorization();

var credential = new DefaultAzureCredential();

// Cosmos DB — shared "jobs" container, System.Text.Json serializer.
builder.Services.AddSingleton(_ =>
{
    var endpoint = builder.Configuration["Cosmos:Endpoint"]
        ?? throw new InvalidOperationException("Cosmos:Endpoint is not configured.");
    var options = new CosmosClientOptions { Serializer = new SystemTextJsonCosmosSerializer() };
    return new CosmosClient(endpoint, credential, options);
});
builder.Services.AddSingleton<IJobRepository>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var database = builder.Configuration["Cosmos:Database"] ?? "transcrib";
    var container = builder.Configuration["Cosmos:Container"] ?? "jobs";
    return new CosmosJobRepository(client.GetContainer(database, container));
});

// Blob Storage — audio + transcripts containers.
builder.Services.AddSingleton(_ =>
{
    var serviceUri = builder.Configuration["Storage:BlobEndpoint"]
        ?? throw new InvalidOperationException("Storage:BlobEndpoint is not configured.");
    return new BlobServiceClient(new Uri(serviceUri), credential);
});
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

builder.Services.AddScoped<JobService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

if (azureAdConfigured)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var jobs = app.MapGroup("/jobs");
if (azureAdConfigured)
{
    jobs.RequireAuthorization();
}

// POST /jobs — upload one or more MP3 files.
jobs.MapPost("/", async (HttpRequest request, JobService service, CancellationToken ct) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "Expected multipart/form-data upload." });
    }

    var form = await request.ReadFormAsync(ct);
    if (form.Files.Count == 0)
    {
        return Results.BadRequest(new { error = "No files were uploaded." });
    }

    var uploads = form.Files
        .Select(f => new UploadFile(f.FileName, f.ContentType, f.OpenReadStream()))
        .ToList();

    var result = await service.CreateJobsAsync(uploads, ct);
    if (result.Created.Count == 0)
    {
        return Results.BadRequest(new { error = "No valid MP3 files were uploaded.", rejected = result.Rejected });
    }

    return Results.Created("/jobs", new { created = result.Created, rejected = result.Rejected });
}).DisableAntiforgery();

// GET /jobs — list all jobs.
jobs.MapGet("/", async (JobService service, CancellationToken ct) =>
    Results.Ok(await service.ListJobsAsync(ct)));

// GET /jobs/{id} — job detail.
jobs.MapGet("/{id}", async (string id, JobService service, CancellationToken ct) =>
{
    var job = await service.GetJobAsync(id, ct);
    return job is null ? Results.NotFound() : Results.Ok(job);
});

// GET /jobs/{id}/transcript — download the generated transcript.
jobs.MapGet("/{id}/transcript", async (string id, JobService service, CancellationToken ct) =>
{
    var transcript = await service.GetTranscriptAsync(id, ct);
    return transcript is null
        ? Results.NotFound(new { error = "Transcript not available." })
        : Results.File(transcript.Value.Content, transcript.Value.ContentType, transcript.Value.FileName);
});

app.Run();

public partial class Program { }
