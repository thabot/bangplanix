using System.Collections.Concurrent;
using Bangplanix.Core.Plugins;
using SysAssembly = System.Reflection.Assembly;

namespace Bangplanix.Engine.Plugins;

public class PluginHostContext : IPluginContext
{
    private readonly string _pluginId;
    private readonly Action<string, string> _logAction;
    private readonly Action<string, Delegate> _registerFunctionAction;

    public PluginHostContext(string pluginId, Action<string, string> logAction, Action<string, Delegate> registerFunctionAction)
    {
        _pluginId = pluginId;
        _logAction = logAction;
        _registerFunctionAction = registerFunctionAction;
    }

    public void LogInformation(string message) => _logAction("INFO", $"[{_pluginId}] {message}");
    public void LogWarning(string message) => _logAction("WARN", $"[{_pluginId}] {message}");
    public void LogError(string message, Exception? ex = null) => _logAction("ERROR", $"[{_pluginId}] {message} {ex?.Message}");
    public void RegisterFunction(string functionName, Delegate functionDelegate) => _registerFunctionAction(functionName, functionDelegate);
}

public class LoadedPluginContainer
{
    public required IReportPlugin Instance { get; init; }
    public required PluginDescriptor Descriptor { get; init; }
    public PluginLoadContext? LoadContext { get; init; }
    public SysAssembly Assembly { get; init; } = null!;
}

public class DynamicPluginManager : IDisposable
{
    private readonly ConcurrentDictionary<string, LoadedPluginContainer> _loadedPlugins = new();
    private readonly ConcurrentDictionary<string, Delegate> _customFunctions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Type> _customConnectors = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IRendererHookPlugin> _rendererHooks = new();
    private readonly List<string> _pluginLogs = new();

    public IReadOnlyList<PluginDescriptor> LoadedPlugins => _loadedPlugins.Values.Select(p => p.Descriptor).ToList();
    public IReadOnlyDictionary<string, Delegate> CustomFunctions => _customFunctions;
    public IReadOnlyDictionary<string, Type> CustomConnectors => _customConnectors;
    public IReadOnlyList<IRendererHookPlugin> RendererHooks
    {
        get
        {
            lock (_rendererHooks)
            {
                return _rendererHooks.ToList();
            }
        }
    }
    public IReadOnlyList<string> Logs
    {
        get
        {
            lock (_pluginLogs)
            {
                return _pluginLogs.ToList();
            }
        }
    }

    public void RegisterDirectPlugin(IReportPlugin pluginInstance)
    {
        ArgumentNullException.ThrowIfNull(pluginInstance);
        var descriptor = pluginInstance.Descriptor;

        var container = new LoadedPluginContainer
        {
            Instance = pluginInstance,
            Descriptor = descriptor,
            Assembly = pluginInstance.GetType().Assembly
        };

        var hostContext = new PluginHostContext(
            descriptor.Id,
            (level, msg) => { lock (_pluginLogs) _pluginLogs.Add($"{DateTime.UtcNow:O} [{level}] {msg}"); },
            (name, del) => _customFunctions[name] = del
        );

        pluginInstance.Initialize(hostContext);

        if (pluginInstance is ICustomFunctionPlugin funcPlugin)
        {
            foreach (var (name, del) in funcPlugin.GetCustomFunctions())
            {
                _customFunctions[name] = del;
            }
        }

        if (pluginInstance is ICustomConnectorPlugin connPlugin)
        {
            foreach (var (name, type) in connPlugin.GetCustomDataConnectors())
            {
                _customConnectors[name] = type;
            }
        }

        if (pluginInstance is IRendererHookPlugin hookPlugin)
        {
            lock (_rendererHooks)
            {
                _rendererHooks.Add(hookPlugin);
            }
        }

        _loadedPlugins[descriptor.Id] = container;
    }

    public LoadedPluginContainer LoadPluginFromAssembly(SysAssembly assembly, PluginLoadContext? loadContext = null, bool validateSecurity = true)
    {
        if (validateSecurity)
        {
            PluginSecurityValidator.ValidateAssembly(assembly);
        }

        var pluginType = assembly.GetExportedTypes()
            .FirstOrDefault(t => typeof(IReportPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            ?? throw new InvalidOperationException($"No class implementing '{nameof(IReportPlugin)}' was found in assembly '{assembly.FullName}'");

        var instance = (IReportPlugin?)Activator.CreateInstance(pluginType)
            ?? throw new InvalidOperationException($"Could not instantiate plugin '{pluginType.FullName}'");

        var descriptor = instance.Descriptor;

        var hostContext = new PluginHostContext(
            descriptor.Id,
            (level, msg) => { lock (_pluginLogs) _pluginLogs.Add($"{DateTime.UtcNow:O} [{level}] {msg}"); },
            (name, del) => _customFunctions[name] = del
        );

        instance.Initialize(hostContext);

        if (instance is ICustomFunctionPlugin funcPlugin)
        {
            foreach (var (name, del) in funcPlugin.GetCustomFunctions())
            {
                _customFunctions[name] = del;
            }
        }

        if (instance is ICustomConnectorPlugin connPlugin)
        {
            foreach (var (name, type) in connPlugin.GetCustomDataConnectors())
            {
                _customConnectors[name] = type;
            }
        }

        if (instance is IRendererHookPlugin hookPlugin)
        {
            lock (_rendererHooks)
            {
                _rendererHooks.Add(hookPlugin);
            }
        }

        var container = new LoadedPluginContainer
        {
            Instance = instance,
            Descriptor = descriptor,
            LoadContext = loadContext,
            Assembly = assembly
        };

        _loadedPlugins[descriptor.Id] = container;
        return container;
    }

    public LoadedPluginContainer LoadPluginFromFile(string pluginDllPath, bool validateSecurity = true)
    {
        if (!File.Exists(pluginDllPath))
        {
            throw new FileNotFoundException("Plugin DLL not found", pluginDllPath);
        }

        var loadContext = new PluginLoadContext(pluginDllPath);
        var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(pluginDllPath));

        return LoadPluginFromAssembly(assembly, loadContext, validateSecurity);
    }

    public bool UnloadPlugin(string pluginId)
    {
        if (!_loadedPlugins.TryRemove(pluginId, out var container))
        {
            return false;
        }

        try
        {
            container.Instance.Shutdown();
        }
        catch
        {
            // Ignore shutdown errors during teardown
        }

        if (container.Instance is IRendererHookPlugin hook)
        {
            lock (_rendererHooks)
            {
                _rendererHooks.Remove(hook);
            }
        }

        // Clean up load context collectible assembly
        container.LoadContext?.Unload();
        return true;
    }

    public void Dispose()
    {
        foreach (var id in _loadedPlugins.Keys.ToList())
        {
            UnloadPlugin(id);
        }
        GC.SuppressFinalize(this);
    }
}
