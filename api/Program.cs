using System.Text.Json;
using System.Threading.RateLimiting;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

// Local dev: load the nearest .env (real environment variables win). Must run before the
// builder so AzureOpenAI__* variables in .env reach the configuration system too.
DotEnv.LoadNearest();

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
// appsettings.json "AzureOpenAI" section, overridable with env vars such as
// AzureOpenAI__Endpoint. AZURE_OPENAI_ENDPOINT (used by tools/ingest) is accepted as a fallback.
var settings = builder.Configuration.GetSection("AzureOpenAI").Get<AiSettings>() ?? new AiSettings();
if (string.IsNullOrWhiteSpace(settings.Endpoint))
    settings.Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint))
    throw new InvalidOperationException(
        "Set AzureOpenAI:Endpoint (or AZURE_OPENAI_ENDPOINT) to your Azure OpenAI endpoint URL.");

// ---------- Knowledge store ----------
var indexPath = ResolveIndexPath(settings.IndexPath);
var store = new KnowledgeStore(indexPath);

// The index records which deployment produced its vectors. Questions must use the same one.
if (!string.Equals(store.EmbeddingDeployment, settings.EmbeddingDeployment, StringComparison.Ordinal))
    throw new InvalidOperationException(
        $"Index was built with embedding deployment '{store.EmbeddingDeployment}' but the API is configured " +
        $"for '{settings.EmbeddingDeployment}'. Re-run tools/ingest or fix the configuration.");

// ---------- Services ----------
// No API keys anywhere: DefaultAzureCredential (az login locally, managed identity in Azure).
var azure = new AzureOpenAIClient(endpoint, new DefaultAzureCredential());

builder.Services.AddSingleton(store);
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    azure.GetEmbeddingClient(settings.EmbeddingDeployment).AsIEmbeddingGenerator());
builder.Services.AddSingleton<IChatClient>(
    azure.GetChatClient(settings.ChatDeployment).AsIChatClient());
builder.Services.AddSingleton<ChatService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(settings.AllowedOrigins)
    .WithHeaders("Content-Type")
    .WithMethods("POST")));

// Basic abuse protection: every call costs money. Behind Azure's proxy, set
// ASPNETCORE_FORWARDEDHEADERS_ENABLED=true so the partition key is the visitor's IP.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("chat", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();
app.UseCors();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok", chunks = store.Count }));

app.MapPost("/api/chat", async (
        ChatRequest request, HttpContext http, ChatService chat, ILogger<ChatService> log, CancellationToken ct) =>
    {
        if (request.Validate() is { } problem)
            return Results.BadRequest(new { error = problem });

        var turns = request.Messages!;

        IReadOnlyList<IndexedChunk> context;
        try
        {
            context = await chat.RetrieveAsync(turns, ct);
        }
        catch (OperationCanceledException)
        {
            return Results.Empty;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Retrieval failed");
            return Results.Json(new { error = "The assistant is unavailable right now." },
                statusCode: StatusCodes.Status502BadGateway);
        }

        // Server-Sent Events: "sources" once, then "delta" per text fragment, then "done".
        var res = http.Response;
        res.ContentType = "text/event-stream";
        res.Headers.CacheControl = "no-cache";
        res.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await WriteEvent(res, "sources", context.Select(c => c.Id), ct);

            await foreach (var update in chat.StreamAnswerAsync(turns, context, ct))
                if (!string.IsNullOrEmpty(update.Text))
                    await WriteEvent(res, "delta", update.Text, ct);

            await WriteEvent(res, "done", new { }, ct);
        }
        catch (OperationCanceledException)
        {
            // Visitor closed the page; nothing to do.
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Streaming failed");
            await WriteEvent(res, "error", "The assistant hit a problem. Please try again.", CancellationToken.None);
        }

        return Results.Empty;
    })
    .RequireRateLimiting("chat");

app.Run();

// ---------- Helpers ----------

static async Task WriteEvent(HttpResponse res, string name, object data, CancellationToken ct)
{
    // JSON-encode the payload so newlines inside text can't break SSE framing.
    await res.WriteAsync($"event: {name}\ndata: {JsonSerializer.Serialize(data)}\n\n", ct);
    await res.Body.FlushAsync(ct);
}

static string ResolveIndexPath(string configured)
{
    if (Path.IsPathRooted(configured) || File.Exists(configured)) return configured;
    var besideBinary = Path.Combine(AppContext.BaseDirectory, configured);
    return File.Exists(besideBinary) ? besideBinary : configured; // let the store report the missing file
}

internal sealed class AiSettings
{
    public string Endpoint { get; set; } = "";
    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";
    public string ChatDeployment { get; set; } = "chat";
    public string IndexPath { get; set; } = "data/index.json";
    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
}
