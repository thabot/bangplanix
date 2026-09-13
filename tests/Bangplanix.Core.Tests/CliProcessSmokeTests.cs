using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

#pragma warning disable CA1707, CA2007

namespace Bangplanix.Core.Tests;

public class CliProcessSmokeTests
{
    private static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "Bangplanix.slnx")) || File.Exists(Path.Combine(current, "Directory.Build.props")))
            {
                return current;
            }
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    [Fact]
    public async Task CliValidateCommand_WithSampleInvoice_ShouldReturnSuccess()
    {
        var repoRoot = FindRepoRoot();
        var samplePath = Path.Combine(repoRoot, "schema", "v1", "samples", "invoice.bpx");

        Assert.True(File.Exists(samplePath), $"Sample file '{samplePath}' must exist.");

        var config = typeof(CliProcessSmokeTests).Assembly.Location.Contains("Release", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project tools/Bangplanix.Cli -c {config} --no-build -- validate -t \"{samplePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repoRoot
        };

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        var output = await proc.StandardOutput.ReadToEndAsync();
        var error = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        proc.ExitCode.Should().Be(0, $"CLI validate should succeed with exit code 0. StdErr: {error}");
        output.Should().Contain("Valid .bpx template");
    }

    [Fact]
    public async Task CliMigrateCommand_WithSsrsRdl_ShouldProduceValidBpxFile()
    {
        var repoRoot = FindRepoRoot();
        var tempDir = Path.Combine(Path.GetTempPath(), "bangplanix_cli_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var rdlPath = Path.Combine(tempDir, "report.rdl");
            var outBpxPath = Path.Combine(tempDir, "report.bpx");

            var rdlContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition">
              <Page>
                <PageWidth>8.5in</PageWidth>
                <PageHeight>11in</PageHeight>
              </Page>
              <Body>
                <Height>2in</Height>
                <ReportItems>
                  <Textbox Name="TitleBox">
                    <Value>CLI Migration Test</Value>
                  </Textbox>
                </ReportItems>
              </Body>
            </Report>
            """;

            await File.WriteAllTextAsync(rdlPath, rdlContent);

            var config = typeof(CliProcessSmokeTests).Assembly.Location.Contains("Release", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project tools/Bangplanix.Cli -c {config} --no-build -- migrate -i \"{rdlPath}\" -o \"{outBpxPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = repoRoot
            };

            using var proc = Process.Start(psi);
            Assert.NotNull(proc);

            var output = await proc.StandardOutput.ReadToEndAsync();
            var error = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            proc.ExitCode.Should().Be(0, $"CLI migrate should succeed. Error: {error}");
            File.Exists(outBpxPath).Should().BeTrue();
            var bpxJson = await File.ReadAllTextAsync(outBpxPath);
            bpxJson.Should().Contain("CLI Migration Test");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
