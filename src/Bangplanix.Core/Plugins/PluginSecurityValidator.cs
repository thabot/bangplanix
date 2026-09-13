using System.Reflection;

namespace Bangplanix.Core.Plugins;

public class PluginSecurityException : Exception
{
    public PluginSecurityException(string message) : base(message) { }
}

public static class PluginSecurityValidator
{
    private static readonly HashSet<string> ProhibitedTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.Diagnostics.Process",
        "System.IO.FileStream",
        "System.Net.Sockets.Socket",
        "System.Runtime.InteropServices.Marshal"
    };

    public static void ValidateAssembly(Assembly assembly)
    {
        var referencedAssemblies = assembly.GetReferencedAssemblies();
        foreach (var refAsm in referencedAssemblies)
        {
            if (refAsm.Name?.Equals("System.Diagnostics.Process", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new PluginSecurityException($"Plugin references prohibited assembly '{refAsm.Name}'");
            }
        }

        // Scan exported types for suspicious direct dangerous attributes
        foreach (var type in assembly.GetExportedTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.GetCustomAttributes().Any(a => a.GetType().Name.Contains("DllImport", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new PluginSecurityException($"Plugin type '{type.FullName}' contains unmanaged DllImport declarations which are prohibited in sandboxed plugins.");
                }
            }
        }
    }
}
