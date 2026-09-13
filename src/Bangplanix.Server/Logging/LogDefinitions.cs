using Microsoft.Extensions.Logging;

namespace Bangplanix.Server.Logging;

public static partial class LogDefinitions
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Bangplanix Server starting on REST port {RestPort} and gRPC port {GrpcPort}...")]
    public static partial void ServerStarting(ILogger logger, int restPort, int grpcPort);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Report rendering requested: Format={Format}, Template={TemplateTitle}, Tenant={TenantId}")]
    public static partial void ReportRenderingRequested(ILogger logger, string format, string templateTitle, string tenantId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Report rendered successfully in {DurationMs} ms: Format={Format}, Size={SizeBytes} bytes")]
    public static partial void ReportRenderedSuccessfully(ILogger logger, long durationMs, string format, int sizeBytes);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "Security alert: Request rejected due to {Reason}")]
    public static partial void SecurityAlert(ILogger logger, string reason);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error, Message = "Report rendering failed: {ErrorMessage}")]
    public static partial void ReportRenderingFailed(ILogger logger, string errorMessage, Exception? ex);
}
