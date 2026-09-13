using System.Reflection;
using System.Runtime.Loader;

namespace Bangplanix.Core.Plugins;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver? _resolver;

    public PluginLoadContext(string? pluginPath = null, string? name = null)
        : base(name ?? (pluginPath != null ? Path.GetFileNameWithoutExtension(pluginPath) : "BangplanixPluginContext"), isCollectible: true)
    {
        if (!string.IsNullOrEmpty(pluginPath) && File.Exists(pluginPath))
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Don't reload core assembly contracts inside isolated context
        if (assemblyName.Name == "Bangplanix.Core" ||
            assemblyName.Name == "Bangplanix.Engine" ||
            assemblyName.Name == "Bangplanix.Connectors" ||
            assemblyName.Name == "Bangplanix.Expressions" ||
            assemblyName.Name?.StartsWith("System.") == true ||
            assemblyName.Name?.StartsWith("Microsoft.") == true)
        {
            return null; // Resolve from default context
        }

        if (_resolver != null)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        if (_resolver != null)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
        }

        return IntPtr.Zero;
    }
}
