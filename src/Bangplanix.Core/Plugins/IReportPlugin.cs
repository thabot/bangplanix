using Bangplanix.Core.Models;

namespace Bangplanix.Core.Plugins;

public class PluginDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Author { get; init; }
    public string? Description { get; init; }
    public bool IsSandboxed { get; init; } = true;
    public DateTimeOffset LoadedAt { get; init; } = DateTimeOffset.UtcNow;
}

public interface IPluginContext
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
    void RegisterFunction(string functionName, Delegate functionDelegate);
}

public interface IReportPlugin
{
    PluginDescriptor Descriptor { get; }
    void Initialize(IPluginContext context);
    void Shutdown();
}

public interface ICustomFunctionPlugin : IReportPlugin
{
    IReadOnlyDictionary<string, Delegate> GetCustomFunctions();
}

public interface ICustomConnectorPlugin : IReportPlugin
{
    IReadOnlyDictionary<string, Type> GetCustomDataConnectors();
}

public interface IRendererHookPlugin : IReportPlugin
{
    Task OnBeforeRenderAsync(ReportDefinition report, IDictionary<string, object?> context, CancellationToken cancellationToken = default);
    Task OnAfterRenderAsync(ReportDefinition report, byte[] outputDocument, IDictionary<string, object?> context, CancellationToken cancellationToken = default);
}
