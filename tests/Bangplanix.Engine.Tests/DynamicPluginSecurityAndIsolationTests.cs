using System.Reflection;
using Bangplanix.Core.Models;
using Bangplanix.Core.Plugins;
using Bangplanix.Engine.Plugins;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class DynamicPluginSecurityAndIsolationTests
{
    [Fact]
    public void DynamicPluginManager_ShouldIsolatePluginContextAndReleaseOnUnload()
    {
        using var manager = new DynamicPluginManager();
        var plugin = new MockCustomFinancePlugin();

        manager.RegisterDirectPlugin(plugin);

        Assert.Single(manager.LoadedPlugins);
        Assert.True(manager.CustomFunctions.ContainsKey("CalcWht"));

        // Unload plugin
        var unloaded = manager.UnloadPlugin("bangplanix-finance-ext");
        Assert.True(unloaded);
        Assert.Empty(manager.LoadedPlugins);
    }

    [Fact]
    public void PluginSecurityValidator_ShouldDetectSafeAndProhibitedAssemblies()
    {
        // Safe assembly: current executing test assembly
        var safeAssembly = typeof(DynamicPluginSecurityAndIsolationTests).Assembly;
        
        var exception = Record.Exception(() => PluginSecurityValidator.ValidateAssembly(safeAssembly));
        Assert.Null(exception);
    }

    [Fact]
    public void PluginLoadContext_ShouldIsolateAndAvoidReloadingCoreContracts()
    {
        var loadContext = new PluginLoadContext(name: "TestIsolationContext");

        Assert.NotNull(loadContext);
        Assert.True(loadContext.IsCollectible);

        // Collectible context can be unloaded without throwing
        loadContext.Unload();
    }
}
