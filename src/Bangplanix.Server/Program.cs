using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bangplanix.Adapters.Common;
using Bangplanix.Connectors.Excel;
using Bangplanix.Connectors.Json;
using Bangplanix.Core.Ai;
using Bangplanix.Core.Distributed;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using Bangplanix.Engine.Ai;
using Bangplanix.Engine.Ai.Providers;
using Bangplanix.Engine.Licensing;
using Bangplanix.Engine.Pdf;
using Bangplanix.Engine.Portal;
using Bangplanix.Engine.Portal.Storage;
using Bangplanix.Server.Logging;
using Bangplanix.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

var restPort = int.TryParse(Environment.GetEnvironmentVariable("BANGPLANIX_HTTP_PORT"), out var hp) ? hp : 9545;
var grpcPort = int.TryParse(Environment.GetEnvironmentVariable("BANGPLANIX_GRPC_PORT"), out var gp) ? gp : 9546;

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/1.1 & HTTP/2 for REST API & Web Viewer
    options.ListenAnyIP(restPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // HTTP/2 for gRPC High-Speed Services
    options.ListenAnyIP(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Configure Database Storage for Portal (SQLite default, PostgreSQL optional)
var dbType = (Environment.GetEnvironmentVariable("BANGPLANIX_PORTAL_DB_TYPE") ?? "sqlite").ToLowerInvariant();
var dbConn = Environment.GetEnvironmentVariable("BANGPLANIX_PORTAL_DB_CONNECTION") 
             ?? (dbType == "sqlite" ? "Data Source=volumes/data/portal.db" : "");

IPortalDatabase portalDb = dbType == "postgres" && !string.IsNullOrWhiteSpace(dbConn)
    ? new PostgreSqlPortalDatabase(dbConn)
    : new SqlitePortalDatabase(string.IsNullOrWhiteSpace(dbConn) ? "Data Source=volumes/data/portal.db" : dbConn);

// Global In-Memory Log Buffer for Portal Live Streaming
var logBuffer = new System.Collections.Concurrent.ConcurrentQueue<string>();
void LogEntry(string msg)
{
    string line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {msg}";
    logBuffer.Enqueue(line);
    while (logBuffer.Count > 200) logBuffer.TryDequeue(out _);
}

LogEntry("Bangplanix Engine starting up (.NET 10 / Alpine Linux)...");

// Commercial License Enforcer
var licenseEnforcer = new CommercialLicenseEnforcer();
string? envLicenseKey = Environment.GetEnvironmentVariable("BANGPLANIX_LICENSE_KEY")
    ?? Environment.GetEnvironmentVariable("LICENSE_KEY");

if (!string.IsNullOrWhiteSpace(envLicenseKey))
{
    var initResult = licenseEnforcer.ApplyLicenseToken(envLicenseKey, Environment.ProcessorCount);
    if (initResult.IsValid)
    {
        LogEntry($"[INFO] Commercial License activated from environment variable. Tier: {initResult.ActiveTier} (Licensed To: {initResult.Payload?.CustomerName ?? "Commercial Customer"})");
    }
    else
    {
        LogEntry($"[WARN] License activation from environment variable failed: {initResult.StatusMessage}");
    }
}

var app = builder.Build();

app.UseCors();

// Initialize Portal Database Schema asynchronously
try
{
    await portalDb.InitializeAsync();
    LogEntry("Portal database initialized successfully.");
}
catch (Exception ex)
{
    LogEntry($"[WARN] Portal DB init: {ex.Message}");
}

// Map gRPC Service Handler
app.MapGrpcService<ReportServiceImpl>();

// Helper: Ensure Safe Path inside /app/volumes or ./volumes
string ResolveSafeVolumePath(string folder, string? fileName = null)
{
    string[] allowed = ["templates", "data", "fonts", "logs"];
    folder = folder.Trim().ToLowerInvariant().Trim('/', '\\');
    if (!allowed.Contains(folder))
    {
        throw new ArgumentException($"Invalid folder category: {folder}. Allowed: templates, data, fonts, logs");
    }

    string basePath = Path.Combine(Directory.GetCurrentDirectory(), "volumes", folder);
    if (!Directory.Exists(basePath))
    {
        Directory.CreateDirectory(basePath);
    }

    if (string.IsNullOrWhiteSpace(fileName)) return basePath;

    // Strict Path Traversal Prevention
    if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
    {
        throw new ArgumentException("Invalid file name. Path traversal characters are strictly prohibited.");
    }

    return Path.Combine(basePath, Path.GetFileName(fileName));
}

// Helper: Check Auth Token
async Task<(bool IsAdmin, string Username)> AuthenticateRequestAsync(HttpContext context)
{
    string? authHeader = context.Request.Headers["Authorization"];
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return (false, "Guest");
    }
    string token = authHeader.Substring("Bearer ".Length).Trim();
    var session = await portalDb.ValidateSessionAsync(token, context.RequestAborted);
    if (session != null)
    {
        return (true, session.Username);
    }
    return (false, "Guest");
}

// --- 1. Root & Management Portal Routes (Content Negotiation) ---
app.MapGet("/", (HttpContext context) =>
{
    var accept = context.Request.Headers.Accept.ToString();
    if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Content(ManagementPortalServer.GetEmbeddedPortalHtml(), "text/html; charset=utf-8");
    }

    return Results.Ok(new
    {
        name = "Bangplanix Report Engine",
        version = "1.0.0-preview.1",
        status = "Online",
        ports = new { rest = restPort, grpc = grpcPort }
    });
});

app.MapGet("/portal", () => Results.Content(ManagementPortalServer.GetEmbeddedPortalHtml(), "text/html; charset=utf-8"));
app.MapGet("/admin", () => Results.Content(ManagementPortalServer.GetEmbeddedPortalHtml(), "text/html; charset=utf-8"));

// --- 2. Telemetry & Metrics Routes ---
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    engine = ".NET 10 / SkiaSharp / HarfBuzz / MiniExcel"
}));

app.MapGet("/metrics", () =>
{
    var p = Process.GetCurrentProcess();
    return Results.Ok(new
    {
        process_uptime_seconds = (long)(DateTime.UtcNow - p.StartTime.ToUniversalTime()).TotalSeconds,
        working_set_bytes = p.WorkingSet64,
        thread_count = p.Threads.Count
    });
});

app.MapGet("/api/metrics", () =>
{
    var p = Process.GetCurrentProcess();
    return Results.Ok(new
    {
        engine = "Bangplanix Enterprise (.NET 10 / Alpine)",
        version = "1.0.0",
        uptimeSeconds = (long)(DateTime.UtcNow - p.StartTime.ToUniversalTime()).TotalSeconds,
        nodeId = Environment.MachineName,
        workerConcurrency = Environment.ProcessorCount,
        totalProcessed = 0,
        totalFailed = 0,
        queueDepth = 0,
        deadLetterCount = 0,
        memoryAllocatedMb = p.WorkingSet64 / (1024.0 * 1024.0),
        timestampUtc = DateTime.UtcNow
    });
});

// --- 3. Portal Authentication API ---
app.MapPost("/api/v1/auth/login", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);
    using var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    string user = root.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "";
    string pass = root.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";

    bool valid = await portalDb.ValidatePasswordAsync(user, pass, context.RequestAborted);
    if (!valid)
    {
        LogEntry($"[WARN] Failed login attempt for user: {user}");
        return Results.Json(new { error = "Invalid username or password." }, statusCode: 401);
    }

    var session = await portalDb.CreateSessionAsync(user, "admin", TimeSpan.FromHours(12), context.RequestAborted);
    await portalDb.LogAuditAsync("USER_LOGIN", user, "Logged in from portal web UI", context.RequestAborted);
    LogEntry($"[INFO] User '{user}' logged in successfully.");

    return Results.Ok(new
    {
        token = session.Token,
        username = session.Username,
        role = session.Role,
        expiresAtUtc = session.ExpiresAtUtc
    });
});

app.MapPost("/api/v1/auth/logout", async (HttpContext context) =>
{
    var (isAdmin, username) = await AuthenticateRequestAsync(context);
    string? authHeader = context.Request.Headers["Authorization"];
    if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        string token = authHeader.Substring("Bearer ".Length).Trim();
        await portalDb.RevokeSessionAsync(token, context.RequestAborted);
    }
    return Results.Ok(new { message = "Logged out successfully." });
});

// --- 4. Container Files Management API ---
app.MapGet("/api/v1/files", (string folder) =>
{
    try
    {
        string dirPath = ResolveSafeVolumePath(folder);
        var dir = new DirectoryInfo(dirPath);
        if (!dir.Exists) return Results.Ok(Array.Empty<object>());

        var files = dir.GetFiles().Select(f => new
        {
            name = f.Name,
            sizeBytes = f.Length,
            lastModifiedUtc = f.LastWriteTimeUtc
        }).ToArray();

        return Results.Ok(files);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/v1/files/download", (string folder, string name) =>
{
    try
    {
        string filePath = ResolveSafeVolumePath(folder, name);
        if (!File.Exists(filePath)) return Results.NotFound(new { error = "File not found." });

        byte[] bytes = File.ReadAllBytes(filePath);
        return Results.File(bytes, "application/octet-stream", name);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/v1/files/upload", async (HttpContext context, [FromQuery] string folder) =>
{
    try
    {
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var file = form.Files.FirstOrDefault();
        if (file == null) return Results.BadRequest(new { error = "No file attached in form-data." });

        string safePath = ResolveSafeVolumePath(folder, file.FileName);
        using (var stream = File.Create(safePath))
        {
            await file.CopyToAsync(stream, context.RequestAborted);
        }

        LogEntry($"[INFO] File '{file.FileName}' uploaded to volumes/{folder}.");
        return Results.Ok(new { success = true, fileName = file.FileName, folder });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/api/v1/files", async (HttpContext context, [FromQuery] string folder, [FromQuery] string name) =>
{
    var (isAdmin, username) = await AuthenticateRequestAsync(context);
    if (!isAdmin)
    {
        return Results.Json(new { error = "Unauthorized. Please log in to delete files." }, statusCode: 401);
    }

    try
    {
        string filePath = ResolveSafeVolumePath(folder, name);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            await portalDb.LogAuditAsync("FILE_DELETE", username, $"Deleted {folder}/{name}", context.RequestAborted);
            LogEntry($"[WARN] File '{folder}/{name}' deleted by '{username}'.");
        }
        return Results.Ok(new { success = true, deleted = name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// --- 5. Web Report Converter API ---
app.MapPost("/api/v1/convert", async (HttpContext context) =>
{
    try
    {
        string content = "";
        string fileName = "report.xml";

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            var file = form.Files.FirstOrDefault();
            if (file != null)
            {
                fileName = file.FileName;
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                content = await reader.ReadToEndAsync(context.RequestAborted);
            }
        }
        else
        {
            using var reader = new StreamReader(context.Request.Body);
            content = await reader.ReadToEndAsync(context.RequestAborted);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Results.BadRequest(new { error = "Report template content cannot be empty." });
        }

        var adapter = LegacyAdapterFactory.GetAdapterByExtension(fileName);
        var report = adapter.Convert(content);
        string bpxJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });

        LogEntry($"[INFO] Successfully converted '{fileName}' via {adapter.FormatName}.");
        return Results.Content(bpxJson, "application/json");
    }
    catch (Exception ex)
    {
        LogEntry($"[ERROR] Report conversion error: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
});

// --- 6. Live Logs Streamer API ---
app.MapGet("/api/v1/logs", () =>
{
    string allLogs = string.Join("\n", logBuffer);
    return Results.Content(allLogs, "text/plain; charset=utf-8");
});

// --- 7. Commercial License Management API ---
app.MapPost("/api/v1/license/activate", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);
    using var doc = JsonDocument.Parse(body);
    string token = doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() ?? "" : "";

    var result = licenseEnforcer.ApplyLicenseToken(token, Environment.ProcessorCount);
    if (!result.IsValid)
    {
        return Results.BadRequest(new { error = result.StatusMessage ?? "Invalid license token." });
    }

    LogEntry($"[INFO] Commercial License activated. Tier: {result.ActiveTier}");
    return Results.Ok(new
    {
        tier = result.ActiveTier.ToString(),
        isValid = result.IsValid,
        message = "License applied successfully."
    });
});

app.MapGet("/api/v1/license/status", () =>
{
    return Results.Ok(new
    {
        tier = licenseEnforcer.CurrentStatus.ActiveTier.ToString(),
        requiresWatermark = licenseEnforcer.RequiresWatermark()
    });
});

// --- 8. Database Connection Health Tester API ---
app.MapPost("/api/v1/tools/db-test", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);
    using var doc = JsonDocument.Parse(body);
    string type = doc.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "postgres";
    string conn = doc.RootElement.TryGetProperty("connectionString", out var cn) ? cn.GetString() ?? "" : "";

    var sw = Stopwatch.StartNew();
    try
    {
        if (type == "sqlite")
        {
            using var c = new Microsoft.Data.Sqlite.SqliteConnection(conn);
            await c.OpenAsync(context.RequestAborted);
        }
        else if (type == "postgres")
        {
            await using var c = new Npgsql.NpgsqlConnection(conn);
            await c.OpenAsync(context.RequestAborted);
        }
        sw.Stop();
        return Results.Ok(new { success = true, latencyMs = sw.ElapsedMilliseconds });
    }
    catch (Exception ex)
    {
        sw.Stop();
        return Results.Ok(new { success = false, error = ex.Message, latencyMs = sw.ElapsedMilliseconds });
    }
});

// --- 9. Environment & Installed Fonts Inspector API ---
app.MapGet("/api/v1/tools/environment", () =>
{
    var fontsDir = Path.Combine(Directory.GetCurrentDirectory(), "volumes", "fonts");
    var fontFiles = Directory.Exists(fontsDir) 
        ? Directory.GetFiles(fontsDir, "*.*").Where(f => f.EndsWith(".ttf") || f.EndsWith(".otf")).Select(Path.GetFileName).ToArray() 
        : Array.Empty<string>();

    return Results.Ok(new
    {
        osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        processArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
        processorCount = Environment.ProcessorCount,
        frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        installedFonts = fontFiles
    });
});

// --- 10. Existing Render to PDF & XLSX Endpoints ---
app.MapPost("/api/v1/render/pdf", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    if (string.IsNullOrWhiteSpace(body))
    {
        return Results.BadRequest(new { error = "Request body with .bpx template cannot be empty." });
    }

    try
    {
        var report = BpxParser.Parse(body);
        var renderer = new SkiaPdfRenderer();
        var defaultData = report.Datasets?.FirstOrDefault()?.StaticData != null
            ? JsonPushStreamConnector.ParseObjectToRows(report.Datasets[0].StaticData!)
            : Array.Empty<IDictionary<string, object?>>();

        var pdfBytes = await renderer.RenderToPdfAsync(report, null, defaultData, cancellationToken: context.RequestAborted);
        LogEntry($"[INFO] Rendered PDF: {report.Metadata?.Title ?? "Report"}");
        return Results.File(pdfBytes, "application/pdf", $"{report.Metadata?.Title ?? "Report"}.pdf");
    }
    catch (Exception ex)
    {
        LogEntry($"[ERROR] PDF Render failed: {ex.Message}");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/v1/render/xlsx", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    if (string.IsNullOrWhiteSpace(body))
    {
        return Results.BadRequest(new { error = "Request body with .bpx template cannot be empty." });
    }

    try
    {
        var report = BpxParser.Parse(body);
        var defaultData = report.Datasets?.FirstOrDefault()?.StaticData != null
            ? JsonPushStreamConnector.ParseObjectToRows(report.Datasets[0].StaticData!)
            : Array.Empty<IDictionary<string, object?>>();

        using var ms = new MemoryStream();
        await MiniExcelReportExporter.ExportReportToExcelAsync(report, defaultData, ms, cancellationToken: context.RequestAborted);
        LogEntry($"[INFO] Exported Excel: {report.Metadata?.Title ?? "Report"}");
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{report.Metadata?.Title ?? "Report"}.xlsx");
    }
    catch (Exception ex)
    {
        LogEntry($"[ERROR] Excel Export failed: {ex.Message}");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// REST: Validate Template
app.MapPost("/api/v1/validate", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    try
    {
        var report = BpxParser.Parse(body);
        return Results.Ok(new
        {
            isValid = true,
            title = report.Metadata?.Title ?? "Untitled",
            version = report.Version,
            bandsCount = (report.Bands?.PageHeader != null ? 1 : 0) + (report.Bands?.Detail != null ? 1 : 0) + (report.Bands?.PageFooter != null ? 1 : 0)
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { isValid = false, error = ex.Message });
    }
});

// ==========================================
// --- 9. Bangplanix AI REST Proxy & Gateway API ---
// ==========================================

HybridLlmGateway ResolveHybridLlmGateway(HttpContext context)
{
    var gateway = new HybridLlmGateway();

    // Check for Client BYOK Headers
    string? byokProvider = context.Request.Headers["X-Bangplanix-Ai-Provider"].FirstOrDefault()?.ToLowerInvariant();
    string? byokKey = context.Request.Headers["X-Bangplanix-Ai-Key"].FirstOrDefault();
    string? byokModel = context.Request.Headers["X-Bangplanix-Ai-Model"].FirstOrDefault();
    string? byokEndpoint = context.Request.Headers["X-Bangplanix-Ai-Endpoint"].FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(byokProvider) && (!string.IsNullOrWhiteSpace(byokKey) || byokProvider == "ollama"))
    {
        switch (byokProvider)
        {
            case "gemini":
                gateway.RegisterProvider(new GeminiLlmProvider(new LlmProviderConfig
                {
                    ApiKey = byokKey ?? "",
                    ModelName = string.IsNullOrWhiteSpace(byokModel) ? "gemini-2.5-flash" : byokModel,
                    EndpointUrl = byokEndpoint,
                    Priority = 1
                }));
                break;

            case "openai":
            case "azure":
            case "azureopenai":
                gateway.RegisterProvider(new AzureOpenAiProvider(new LlmProviderConfig
                {
                    ApiKey = byokKey ?? "",
                    ModelName = string.IsNullOrWhiteSpace(byokModel) ? "gpt-4o" : byokModel,
                    EndpointUrl = byokEndpoint,
                    Priority = 1
                }));
                break;

            case "claude":
            case "anthropic":
                gateway.RegisterProvider(new AnthropicClaudeProvider(new LlmProviderConfig
                {
                    ApiKey = byokKey ?? "",
                    ModelName = string.IsNullOrWhiteSpace(byokModel) ? "claude-3-5-sonnet-20241022" : byokModel,
                    EndpointUrl = byokEndpoint,
                    Priority = 1
                }));
                break;

            case "ollama":
                gateway.RegisterProvider(new OllamaLlmProvider(new LlmProviderConfig
                {
                    ApiKey = "local",
                    ModelName = string.IsNullOrWhiteSpace(byokModel) ? "llama3" : byokModel,
                    EndpointUrl = string.IsNullOrWhiteSpace(byokEndpoint) ? "http://localhost:11434/api/generate" : byokEndpoint,
                    Priority = 1
                }));
                break;
        }
    }
    else
    {
        // Server Environment fallback
        string? envGeminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envGeminiKey))
        {
            gateway.RegisterProvider(new GeminiLlmProvider(new LlmProviderConfig
            {
                ApiKey = envGeminiKey,
                ModelName = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-2.5-flash",
                Priority = 1
            }));
        }
    }

    return gateway;
}

// AI: Generate .bpx Schema & Safe SQL
app.MapPost("/api/v1/ai/generate", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);
    
    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string prompt = root.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "";
        string tenantId = root.TryGetProperty("tenantId", out var t) ? t.GetString() ?? "default" : "default";
        string? originalBpxSnapshot = root.TryGetProperty("originalBpxSnapshot", out var orig) ? orig.GetString() : null;

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return Results.BadRequest(new { success = false, error = "Prompt cannot be empty" });
        }

        var gateway = ResolveHybridLlmGateway(context);
        var generator = new ReportAiGenerator(gateway);
        var result = await generator.GenerateReportAsync(prompt, tenantId: tenantId, cancellationToken: context.RequestAborted);

        if (result.Success)
        {
            // Extract Provider and Model info for history
            string providerName = context.Request.Headers["X-Bangplanix-Ai-Provider"].FirstOrDefault() ?? "default";
            string modelName = context.Request.Headers["X-Bangplanix-Ai-Model"].FirstOrDefault() ?? "gemini-2.5-flash";

            var (isAdmin, username) = await AuthenticateRequestAsync(context);
            string user = string.IsNullOrWhiteSpace(username) ? "guest" : username;

            long historyId = 0;
            try
            {
                historyId = await portalDb.SaveAiHistoryAsync(
                    user,
                    prompt,
                    providerName,
                    modelName,
                    originalBpxSnapshot,
                    result.BpxJson,
                    result.GeneratedSql,
                    context.RequestAborted);
            }
            catch (Exception ex)
            {
                LogEntry($"[WARN] Could not save AI history: {ex.Message}");
            }

            LogEntry($"[INFO] AI generated template successfully for '{prompt}' in {result.Elapsed.TotalMilliseconds:F1}ms");
            return Results.Ok(new
            {
                success = true,
                historyId = historyId,
                bpxJson = result.BpxJson,
                reportDefinition = result.ReportDefinition,
                generatedSql = result.GeneratedSql,
                elapsedMs = result.Elapsed.TotalMilliseconds
            });
        }
        else
        {
            LogEntry($"[WARN] AI generation failed: {result.ErrorMessage}");
            return Results.Json(new { success = false, error = result.ErrorMessage }, statusCode: 400);
        }
    }
    catch (Exception ex)
    {
        LogEntry($"[ERROR] AI generate error: {ex.Message}");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// AI: Expression Assistant
app.MapPost("/api/v1/ai/expression", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string intent = root.TryGetProperty("intent", out var i) ? i.GetString() ?? "" : "";
        List<string>? fields = null;
        if (root.TryGetProperty("availableFields", out var f) && f.ValueKind == JsonValueKind.Array)
        {
            fields = f.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        var gateway = ResolveHybridLlmGateway(context);
        var assistant = new AiExpressionAssistant(gateway);
        var result = await assistant.GenerateExpressionAsync(intent, fields, cancellationToken: context.RequestAborted);

        if (result.Success)
        {
            return Results.Ok(new
            {
                success = true,
                expression = result.CsharpExpression,
                returnType = result.ReturnType,
                explanation = result.Explanation
            });
        }
        else
        {
            return Results.BadRequest(new { success = false, error = result.ErrorMessage });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// AI: Self-Healing Diagnose and Repair
app.MapPost("/api/v1/ai/diagnose-repair", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string bpxJson = root.TryGetProperty("bpxJson", out var b) ? b.GetString() ?? "" : "";
        string? errorMsg = root.TryGetProperty("errorMessage", out var e) ? e.GetString() : null;

        var gateway = ResolveHybridLlmGateway(context);
        var agent = new SelfHealingReportAgent(gateway);
        var result = agent.DiagnoseAndRepair(bpxJson, errorMsg);

        return Results.Ok(new
        {
            success = result.IsRepaired,
            rootCause = result.RootCause,
            fixSummary = result.FixSummary,
            repairedBpxJson = result.PatchedBpxJson,
            appliedFixes = result.AppliedFixes
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// AI: Executive Summary & Outliers
app.MapPost("/api/v1/ai/executive-summary", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var dataset = new List<IDictionary<string, object?>>();
        if (root.TryGetProperty("dataset", out var ds) && ds.ValueKind == JsonValueKind.Array)
        {
            dataset = JsonPushStreamConnector.ParseObjectToRows(ds.GetRawText()).ToList();
        }

        var metricSeries = new Dictionary<string, IReadOnlyList<double>>();
        if (root.TryGetProperty("metricColumns", out var mc) && mc.ValueKind == JsonValueKind.Array)
        {
            foreach (var colElem in mc.EnumerateArray())
            {
                string col = colElem.GetString() ?? "";
                if (!string.IsNullOrEmpty(col))
                {
                    var vals = dataset
                        .Where(row => row.TryGetValue(col, out var v) && v != null)
                        .Select(row => Convert.ToDouble(row[col]))
                        .ToList();
                    metricSeries[col] = vals;
                }
            }
        }

        string language = root.TryGetProperty("language", out var lang) ? lang.GetString() ?? "th" : "th";
        string title = root.TryGetProperty("title", out var tit) ? tit.GetString() ?? "Executive Report" : "Executive Report";

        var gateway = ResolveHybridLlmGateway(context);
        var engine = new AiExecutiveSummaryEngine(gateway);
        var result = await engine.GenerateSummaryAsync(title, metricSeries, language, cancellationToken: context.RequestAborted);

        return Results.Ok(new
        {
            success = true,
            headline = result.Headline,
            formattedMarkdown = result.FormattedMarkdown,
            keyHighlights = result.KeyHighlights,
            anomalyWarnings = result.AnomalyWarnings,
            recommendations = result.StrategicRecommendations,
            metricSnapshots = result.MetricSnapshots
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// AI: Test Connection (Ping API Key & Model)
app.MapPost("/api/v1/ai/test-connection", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string provider = root.TryGetProperty("provider", out var p) ? p.GetString()?.ToLowerInvariant() ?? "gemini" : "gemini";
        string apiKey = root.TryGetProperty("apiKey", out var k) ? k.GetString() ?? "" : "";
        string modelName = root.TryGetProperty("modelName", out var m) ? m.GetString() ?? "" : "";
        string? endpoint = root.TryGetProperty("endpointUrl", out var ep) ? ep.GetString() : null;

        var sw = Stopwatch.StartNew();
        var gateway = new HybridLlmGateway();
        ILlmProvider? llmProvider = provider switch
        {
            "gemini" => new GeminiLlmProvider(new LlmProviderConfig { ApiKey = apiKey, ModelName = string.IsNullOrWhiteSpace(modelName) ? "gemini-2.5-flash" : modelName, EndpointUrl = endpoint }),
            "openai" or "azure" or "azureopenai" => new AzureOpenAiProvider(new LlmProviderConfig { ApiKey = apiKey, ModelName = string.IsNullOrWhiteSpace(modelName) ? "gpt-4o" : modelName, EndpointUrl = endpoint }),
            "claude" or "anthropic" => new AnthropicClaudeProvider(new LlmProviderConfig { ApiKey = apiKey, ModelName = string.IsNullOrWhiteSpace(modelName) ? "claude-3-5-sonnet-20241022" : modelName, EndpointUrl = endpoint }),
            "ollama" => new OllamaLlmProvider(new LlmProviderConfig { ApiKey = "local", ModelName = string.IsNullOrWhiteSpace(modelName) ? "llama3" : modelName, EndpointUrl = string.IsNullOrWhiteSpace(endpoint) ? "http://localhost:11434/api/generate" : endpoint }),
            _ => null
        };

        if (llmProvider == null)
        {
            return Results.BadRequest(new { success = false, error = $"Unknown provider '{provider}'" });
        }

        gateway.RegisterProvider(llmProvider);
        var testPrompt = new LlmPrompt
        {
            Messages = new List<LlmMessage> { new(LlmRole.User, "Ping. Respond with 'PONG' in plain text.") },
            MaxTokens = 10
        };

        var response = await gateway.GenerateCompletionAsync(testPrompt, context.RequestAborted);
        sw.Stop();

        if (response.Success)
        {
            return Results.Ok(new
            {
                success = true,
                provider = provider,
                model = modelName,
                latencyMs = sw.Elapsed.TotalMilliseconds,
                message = "Connection test successful"
            });
        }
        else
        {
            return Results.Json(new
            {
                success = false,
                provider = provider,
                latencyMs = sw.Elapsed.TotalMilliseconds,
                error = response.ErrorMessage
            }, statusCode: 400);
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// AI: History & Undo
app.MapGet("/api/v1/ai/history", async (HttpContext context) =>
{
    var (isAdmin, username) = await AuthenticateRequestAsync(context);
    string? queryUser = isAdmin ? null : (string.IsNullOrWhiteSpace(username) ? "guest" : username);

    int limit = int.TryParse(context.Request.Query["limit"], out var l) ? l : 50;
    var history = await portalDb.GetAiHistoryAsync(queryUser, limit, context.RequestAborted);

    return Results.Ok(history.Select(h => new
    {
        id = h.Id,
        username = h.Username,
        prompt = h.PromptText,
        provider = h.Provider,
        modelName = h.ModelName,
        hasSnapshot = !string.IsNullOrEmpty(h.OriginalBpxSnapshot),
        generatedSql = h.GeneratedSql,
        createdAtUtc = h.CreatedAtUtc
    }));
});

app.MapPost("/api/v1/ai/history/undo", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        long id = root.TryGetProperty("historyId", out var h) ? h.GetInt64() : 0;

        if (id <= 0) return Results.BadRequest(new { error = "Invalid historyId" });

        var item = await portalDb.GetAiHistoryItemAsync(id, context.RequestAborted);
        if (item == null) return Results.NotFound(new { error = "History snapshot not found" });

        if (string.IsNullOrEmpty(item.OriginalBpxSnapshot))
        {
            return Results.BadRequest(new { error = "No original snapshot available for this history item" });
        }

        return Results.Ok(new
        {
            success = true,
            historyId = item.Id,
            restoredBpxJson = item.OriginalBpxSnapshot
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// ==========================================
// --- 10. Bangplanix MCP (Model Context Protocol) Server ---
// ==========================================

app.MapMethods("/api/v1/mcp", new[] { "GET", "POST" }, async (HttpContext context) =>
{
    if (context.Request.Method == "GET")
    {
        return Results.Ok(new
        {
            name = "bangplanix-mcp-server",
            version = "1.0.0",
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new
                {
                    listChanged = false
                }
            },
            tools = new object[]
            {
                new
                {
                    name = "generate_report_bpx",
                    description = "Generate a valid Bangplanix .bpx JSON template and safe SQL query from natural language.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            prompt = new { type = "string", description = "Natural language prompt in Thai or English" }
                        },
                        required = new[] { "prompt" }
                    }
                },
                new
                {
                    name = "validate_report_bpx",
                    description = "Validate a .bpx JSON schema structure.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bpxJson = new { type = "string", description = ".bpx JSON string" }
                        },
                        required = new[] { "bpxJson" }
                    }
                },
                new
                {
                    name = "render_report_pdf",
                    description = "Render a .bpx template to a PDF document.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bpxJson = new { type = "string", description = ".bpx JSON string" },
                            jsonData = new { type = "string", description = "Optional JSON rows array" }
                        },
                        required = new[] { "bpxJson" }
                    }
                },
                new
                {
                    name = "repair_report_bpx",
                    description = "Auto-repair corrupted or invalid .bpx schema.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            bpxJson = new { type = "string", description = "Broken JSON string" }
                        },
                        required = new[] { "bpxJson" }
                    }
                },
                new
                {
                    name = "list_container_templates",
                    description = "List available report templates stored in container /app/volumes/templates.",
                    inputSchema = new { type = "object" }
                }
            }
        });
    }

    // POST: Execute MCP Tool Call
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);
    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string toolName = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        var arguments = root.TryGetProperty("arguments", out var a) ? a : default;

        var gateway = ResolveHybridLlmGateway(context);

        switch (toolName)
        {
            case "generate_report_bpx":
            {
                string prompt = arguments.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "";
                var generator = new ReportAiGenerator(gateway);
                var result = await generator.GenerateReportAsync(prompt, cancellationToken: context.RequestAborted);
                return Results.Ok(new
                {
                    content = new[]
                    {
                        new { type = "text", text = result.BpxJson }
                    },
                    isError = !result.Success
                });
            }

            case "validate_report_bpx":
            {
                string bpx = arguments.TryGetProperty("bpxJson", out var b) ? b.GetString() ?? "" : "";
                var report = BpxParser.Parse(bpx);
                return Results.Ok(new
                {
                    content = new[]
                    {
                        new { type = "text", text = $"Valid template: {report.Metadata?.Title ?? "Untitled"} (v{report.Version})" }
                    }
                });
            }

            case "repair_report_bpx":
            {
                string bpx = arguments.TryGetProperty("bpxJson", out var b) ? b.GetString() ?? "" : "";
                var agent = new SelfHealingReportAgent(gateway);
                var result = agent.DiagnoseAndRepair(bpx);
                return Results.Ok(new
                {
                    content = new[]
                    {
                        new { type = "text", text = result.PatchedBpxJson }
                    },
                    isError = !result.IsRepaired
                });
            }

            case "list_container_templates":
            {
                string tplPath = ResolveSafeVolumePath("templates");
                var files = Directory.Exists(tplPath) 
                    ? Directory.GetFiles(tplPath, "*.bpx").Select(Path.GetFileName).ToArray()
                    : Array.Empty<string?>();
                return Results.Ok(new
                {
                    content = new[]
                    {
                        new { type = "text", text = JsonSerializer.Serialize(files) }
                    }
                });
            }

            default:
                return Results.BadRequest(new { error = $"Unknown MCP Tool: {toolName}" });
        }
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            content = new[] { new { type = "text", text = $"Error: {ex.Message}" } },
            isError = true
        });
    }
});

app.Run();
