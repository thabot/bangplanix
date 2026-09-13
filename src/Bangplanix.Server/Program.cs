using System.Text.Json;
using Bangplanix.Connectors.Excel;
using Bangplanix.Connectors.Json;
using Bangplanix.Core.Parser;
using Bangplanix.Engine.Pdf;
using Bangplanix.Server.Logging;
using Bangplanix.Server.Services;
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

var app = builder.Build();

app.UseCors();

// Map gRPC Service Handler
app.MapGrpcService<ReportServiceImpl>();

// REST / Web Viewer API Endpoints
app.MapGet("/", () => Results.Ok(new
{
    name = "Bangplanix Report Engine",
    version = "1.0.0-preview.1",
    status = "Online",
    ports = new { rest = restPort, grpc = grpcPort }
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    engine = ".NET 10 / SkiaSharp / HarfBuzz / MiniExcel"
}));

app.MapGet("/metrics", () => Results.Ok(new
{
    process_uptime_seconds = (long)(DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds,
    working_set_bytes = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,
    thread_count = System.Diagnostics.Process.GetCurrentProcess().Threads.Count
}));

// REST: Render to PDF
app.MapPost("/api/v1/render/pdf", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);

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

        var pdfBytes = await renderer.RenderToPdfAsync(report, null, defaultData, cancellationToken: context.RequestAborted).ConfigureAwait(false);
        return Results.File(pdfBytes, "application/pdf", $"{report.Metadata?.Title ?? "Report"}.pdf");
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// REST: Render to Excel (XLSX)
app.MapPost("/api/v1/render/xlsx", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);

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
        await MiniExcelReportExporter.ExportReportToExcelAsync(report, defaultData, ms, cancellationToken: context.RequestAborted).ConfigureAwait(false);
        return Results.File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{report.Metadata?.Title ?? "Report"}.xlsx");
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// REST: Validate Template
app.MapPost("/api/v1/validate", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);

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

app.Run();
