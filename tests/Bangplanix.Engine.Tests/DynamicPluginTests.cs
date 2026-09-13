using Bangplanix.Core.Models;
using Bangplanix.Core.Plugins;
using Bangplanix.Engine.Plugins;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class MockCustomFinancePlugin : ICustomFunctionPlugin, IRendererHookPlugin
{
    public PluginDescriptor Descriptor => new()
    {
        Id = "bangplanix-finance-ext",
        Name = "Thai Corporate Finance Extension Plugin",
        Version = "1.0.0",
        Author = "Bangplanix Enterprise Team",
        Description = "Calculates Corporate Withholding Tax and Progressive Tax rates"
    };

    public bool WasInitialized { get; private set; }
    public bool BeforeRenderHookExecuted { get; private set; }

    public void Initialize(IPluginContext context)
    {
        WasInitialized = true;
        context.LogInformation("Finance plugin initialized successfully.");
    }

    public void Shutdown()
    {
        WasInitialized = false;
    }

    public IReadOnlyDictionary<string, Delegate> GetCustomFunctions()
    {
        return new Dictionary<string, Delegate>
        {
            ["CalcWht"] = new Func<double, double, double>((amount, rate) => amount * (rate / 100.0)),
            ["CorporateTaxEstimate"] = new Func<double, double>(revenue => revenue > 3_000_000 ? revenue * 0.20 : revenue * 0.15)
        };
    }

    public Task OnBeforeRenderAsync(ReportDefinition report, IDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        BeforeRenderHookExecuted = true;
        return Task.CompletedTask;
    }

    public Task OnAfterRenderAsync(ReportDefinition report, byte[] outputDocument, IDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class DynamicPluginTests
{
    [Fact]
    public void DynamicPluginManager_ShouldRegisterAndExecuteCustomPluginFunctions()
    {
        using var manager = new DynamicPluginManager();
        var plugin = new MockCustomFinancePlugin();

        manager.RegisterDirectPlugin(plugin);

        Assert.True(plugin.WasInitialized);
        Assert.Single(manager.LoadedPlugins);
        Assert.Equal("bangplanix-finance-ext", manager.LoadedPlugins[0].Id);

        // Verify Custom Function Registration
        Assert.True(manager.CustomFunctions.ContainsKey("CalcWht"));
        Assert.True(manager.CustomFunctions.ContainsKey("CorporateTaxEstimate"));

        var calcWht = (Func<double, double, double>)manager.CustomFunctions["CalcWht"];
        var wht = calcWht(100000.0, 3.0); // 3% of 100,000 = 3,000
        Assert.Equal(3000.0, wht);

        var corpTax = (Func<double, double>)manager.CustomFunctions["CorporateTaxEstimate"];
        Assert.Equal(150000.0, corpTax(1_000_000.0)); // 15%
        Assert.Equal(1000000.0, corpTax(5_000_000.0)); // 20%

        // Verify Renderer Hooks
        Assert.Single(manager.RendererHooks);

        // Verify Unloading
        var unloaded = manager.UnloadPlugin("bangplanix-finance-ext");
        Assert.True(unloaded);
        Assert.False(plugin.WasInitialized);
        Assert.Empty(manager.LoadedPlugins);
    }

    [Fact]
    public void DynamicPluginManager_ShouldSupportAssemblyLoadingAndSecurityValidation()
    {
        using var manager = new DynamicPluginManager();

        // Load plugin from current executing assembly directly
        var currentAssembly = typeof(MockCustomFinancePlugin).Assembly;
        var container = manager.LoadPluginFromAssembly(currentAssembly, validateSecurity: false);

        Assert.NotNull(container);
        Assert.Equal("bangplanix-finance-ext", container.Descriptor.Id);
        Assert.Contains("CalcWht", manager.CustomFunctions.Keys);
    }
}
