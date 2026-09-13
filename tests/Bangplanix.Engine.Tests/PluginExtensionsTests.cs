using System.Text;
using System.Text.Json;
using Bangplanix.Core.Plugins;
using Bangplanix.Engine.Plugins;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class PluginExtensionsTests
{
    [Fact]
    public void PluginSignatureValidator_ShouldCreateAndVerifyValidSignature()
    {
        var dummyAssemblyBytes = Encoding.UTF8.GetBytes("MOCK_ASSEMBLY_IL_PAYLOAD_BANGPLANIX_ENTERPRISE");
        var secretKey = Encoding.UTF8.GetBytes("super-secret-enterprise-ci-cd-key-999");

        var sigInfo = PluginSignatureValidator.CreateSignature(dummyAssemblyBytes, "thabot-verified-publisher", secretKey);

        Assert.NotNull(sigInfo);
        Assert.Equal("thabot-verified-publisher", sigInfo.PublisherId);
        Assert.Equal("HMAC-SHA256", sigInfo.Algorithm);

        // Verify valid signature
        var isValid = PluginSignatureValidator.VerifySignature(dummyAssemblyBytes, sigInfo, secretKey);
        Assert.True(isValid);

        // Verify tampering detection
        var tamperedBytes = Encoding.UTF8.GetBytes("MOCK_ASSEMBLY_IL_PAYLOAD_TAMPERED_MALICIOUS");
        var isTamperedValid = PluginSignatureValidator.VerifySignature(tamperedBytes, sigInfo, secretKey);
        Assert.False(isTamperedValid);

        // Verify invalid key detection
        var wrongKey = Encoding.UTF8.GetBytes("wrong-key-attack");
        var isWrongKeyValid = PluginSignatureValidator.VerifySignature(dummyAssemblyBytes, sigInfo, wrongKey);
        Assert.False(isWrongKeyValid);
    }

    [Fact]
    public void PluginDirectoryWatcher_ShouldRejectUnsignedPluginsWhenKeyConfigured()
    {
        using var manager = new DynamicPluginManager();
        var tempPluginDir = Path.Combine(Path.GetTempPath(), $"bpx_test_plugins_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPluginDir);

        var secretKey = Encoding.UTF8.GetBytes("enterprise-signing-secret");

        try
        {
            using var watcher = new PluginDirectoryWatcher(manager, tempPluginDir, verificationKey: secretKey, startImmediately: false);

            string? reportedError = null;
            watcher.OnPluginLoadError += (path, err) => reportedError = err;

            // Create unsigned dummy dll
            var unsignedDll = Path.Combine(tempPluginDir, "UnsignedPlugin.dll");
            File.WriteAllText(unsignedDll, "dummy-dll-content");

            var loaded = watcher.TryLoadPluginFile(unsignedDll);
            Assert.False(loaded);
            Assert.NotNull(reportedError);
            Assert.Contains("Missing signature file", reportedError);
        }
        finally
        {
            if (Directory.Exists(tempPluginDir))
            {
                Directory.Delete(tempPluginDir, recursive: true);
            }
        }
    }
}
