using System.Diagnostics;
using Bangplanix.Connectors.Excel;
using Bangplanix.Connectors.Json;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using Bangplanix.Engine.Pdf;
using Bangplanix.Server.Grpc;
using Bangplanix.Server.Logging;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Bangplanix.Server.Services;

public sealed class ReportServiceImpl : BangplanixReportService.BangplanixReportServiceBase
{
    private readonly ILogger<ReportServiceImpl> _logger;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public ReportServiceImpl(ILogger<ReportServiceImpl> logger)
    {
        _logger = logger;
    }

    public override async Task<RenderReportResponse> RenderReport(
        RenderReportRequest request,
        ServerCallContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var report = ResolveReport(request);
            LogDefinitions.ReportRenderingRequested(_logger, request.Format.ToString(), report.Metadata?.Title ?? "Untitled", request.TenantId ?? "default");

            // Extract or inject data rows
            var dataRows = ResolveDataRows(request, report);

            byte[] payload;
            string contentType;
            string filename;
            int pageCount = 1;

            if (request.Format == OutputFormat.Xlsx)
            {
                using var ms = new MemoryStream();
                await MiniExcelReportExporter.ExportReportToExcelAsync(report, dataRows, ms, cancellationToken: context.CancellationToken).ConfigureAwait(false);
                payload = ms.ToArray();
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                filename = $"{SanitizeFilename(report.Metadata?.Title ?? "Report")}.xlsx";
            }
            else
            {
                // Default PDF
                var renderer = new SkiaPdfRenderer();
                payload = await renderer.RenderToPdfAsync(report, null, dataRows, cancellationToken: context.CancellationToken).ConfigureAwait(false);
                contentType = "application/pdf";
                filename = $"{SanitizeFilename(report.Metadata?.Title ?? "Report")}.pdf";
            }

            sw.Stop();
            LogDefinitions.ReportRenderedSuccessfully(_logger, sw.ElapsedMilliseconds, request.Format.ToString(), payload.Length);

            return new RenderReportResponse
            {
                Payload = ByteString.CopyFrom(payload),
                ContentType = contentType,
                DurationMs = sw.ElapsedMilliseconds,
                PageCount = pageCount,
                Filename = filename
            };
        }
        catch (Exception ex)
        {
            LogDefinitions.ReportRenderingFailed(_logger, ex.Message, ex);
            throw new RpcException(new Status(StatusCode.Internal, $"Render failed: {ex.Message}"));
        }
    }

    public override async Task RenderReportStream(
        RenderReportRequest request,
        IServerStreamWriter<StreamReportChunk> responseStream,
        ServerCallContext context)
    {
        var reportResponse = await RenderReport(request, context).ConfigureAwait(false);
        var payloadBytes = reportResponse.Payload.Memory;

        const int chunkSize = 64 * 1024; // 64 KB streaming chunks
        int totalBytes = payloadBytes.Length;
        int offset = 0;
        long sequence = 0;

        while (offset < totalBytes)
        {
            int currentChunk = Math.Min(chunkSize, totalBytes - offset);
            var slice = payloadBytes.Slice(offset, currentChunk);
            bool isFinal = (offset + currentChunk) >= totalBytes;

            await responseStream.WriteAsync(new StreamReportChunk
            {
                Chunk = ByteString.CopyFrom(slice.Span),
                Sequence = sequence++,
                IsFinal = isFinal
            }).ConfigureAwait(false);

            offset += currentChunk;
        }
    }

    public override Task<ValidateTemplateResponse> ValidateTemplate(
        ValidateTemplateRequest request,
        ServerCallContext context)
    {
        try
        {
            var json = !string.IsNullOrWhiteSpace(request.TemplateJson)
                ? request.TemplateJson
                : (File.Exists(request.TemplatePath) ? File.ReadAllText(request.TemplatePath) : throw new FileNotFoundException($"Template '{request.TemplatePath}' not found."));

            var report = BpxParser.Parse(json);
            return Task.FromResult(new ValidateTemplateResponse
            {
                IsValid = true,
                Title = report.Metadata?.Title ?? "Untitled",
                Version = report.Version
            });
        }
        catch (Exception ex)
        {
            var response = new ValidateTemplateResponse
            {
                IsValid = false,
                Title = string.Empty,
                Version = string.Empty
            };
            response.Errors.Add(ex.Message);
            return Task.FromResult(response);
        }
    }

    public override Task<HealthCheckResponse> HealthCheck(
        HealthCheckRequest request,
        ServerCallContext context)
    {
        var uptime = (long)(DateTime.UtcNow - _startTime).TotalSeconds;
        return Task.FromResult(new HealthCheckResponse
        {
            Status = HealthCheckResponse.Types.ServingStatus.Serving,
            Version = "1.0.0-preview.1",
            UptimeSeconds = uptime
        });
    }

    private static ReportDefinition ResolveReport(RenderReportRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.TemplateJson))
        {
            return BpxParser.Parse(request.TemplateJson);
        }

        if (!string.IsNullOrWhiteSpace(request.TemplatePath))
        {
            var path = request.TemplatePath;
            if (!File.Exists(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "volumes", "templates", request.TemplatePath);
            }

            if (File.Exists(path))
            {
                return BpxParser.Parse(File.ReadAllText(path));
            }
        }

        throw new ArgumentException("Either template_json or a valid template_path must be specified.");
    }

    private static IReadOnlyList<IDictionary<string, object?>> ResolveDataRows(RenderReportRequest request, ReportDefinition report)
    {
        if (!string.IsNullOrWhiteSpace(request.DataJson))
        {
            return JsonPushStreamConnector.ParseJsonStringToRows(request.DataJson);
        }

        var defaultDataset = report.Datasets?.FirstOrDefault();
        if (defaultDataset?.StaticData != null)
        {
            return JsonPushStreamConnector.ParseObjectToRows(defaultDataset.StaticData);
        }

        return Array.Empty<IDictionary<string, object?>>();
    }

    private static string SanitizeFilename(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(input.Select(c => invalid.Contains(c) ? '_' : c)).Replace(' ', '_');
    }
}
