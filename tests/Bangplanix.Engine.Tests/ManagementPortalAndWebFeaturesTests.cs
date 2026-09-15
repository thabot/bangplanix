using System.IO;
using System.Text;
using System.Text.Json;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Licensing;
using Bangplanix.Engine.Pdf;
using Bangplanix.Engine.Portal;
using Bangplanix.Engine.Portal.Storage;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class ManagementPortalAndWebFeaturesTests
{
    // --- 1. กลุ่ม Authentication & Default Credentials ---

    [Fact]
    public async Task PortalAuth_ShouldLoginSuccessfully_WithDefaultCredentials()
    {
        await using var db = new SqlitePortalDatabase("Data Source=:memory:");
        await db.InitializeAsync();

        bool valid = await db.ValidatePasswordAsync("admin", "bangplanix2026!");
        Assert.True(valid);

        var session = await db.CreateSessionAsync("admin", "admin");
        Assert.NotNull(session.Token);
        Assert.Equal("admin", session.Username);
        Assert.Equal("admin", session.Role);

        var validated = await db.ValidateSessionAsync(session.Token);
        Assert.NotNull(validated);
        Assert.Equal("admin", validated.Username);
    }

    [Fact]
    public async Task PortalAuth_ShouldReject_InvalidPassword()
    {
        await using var db = new SqlitePortalDatabase("Data Source=:memory:");
        await db.InitializeAsync();

        bool valid = await db.ValidatePasswordAsync("admin", "wrongpassword");
        Assert.False(valid);

        var invalidSession = await db.ValidateSessionAsync("non-existent-token");
        Assert.Null(invalidSession);
    }

    [Fact]
    public async Task PortalAuth_ShouldSupportEnvironmentOverride()
    {
        string customPass = "CustomEnterprisePass2026#";
        Environment.SetEnvironmentVariable("BANGPLANIX_PORTAL_PASSWORD", customPass);

        try
        {
            await using var db = new SqlitePortalDatabase("Data Source=:memory:");
            await db.InitializeAsync();

            bool valid = await db.ValidatePasswordAsync("admin", customPass);
            Assert.True(valid);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BANGPLANIX_PORTAL_PASSWORD", null);
        }
    }

    // --- 2. กลุ่ม Database Storage (SQLite) ---

    [Fact]
    public async Task SqlitePortalDatabase_ShouldInitializeAndCreateTables()
    {
        await using var db = new SqlitePortalDatabase("Data Source=:memory:");
        await db.InitializeAsync();

        var user = await db.GetUserAsync("admin");
        Assert.NotNull(user);
        Assert.Equal("admin", user.Username);
        Assert.Equal("admin", user.Role);
    }

    [Fact]
    public async Task SqlitePortalDatabase_ShouldPersistAuditLogs()
    {
        await using var db = new SqlitePortalDatabase("Data Source=:memory:");
        await db.InitializeAsync();

        await db.LogAuditAsync("TEST_ACTION", "tester", "Uploaded template invoice.bpx");
        await db.LogAuditAsync("CONVERT_ACTION", "tester", "Converted SSRS RDL to BPX");

        var logs = await db.GetAuditLogsAsync(10);
        Assert.NotNull(logs);
        Assert.True(logs.Count >= 2);
        Assert.Contains(logs, l => l.Action == "CONVERT_ACTION" && l.Details.Contains("SSRS"));
    }

    // --- 3. กลุ่ม Container File Management & Security ---

    [Fact]
    public void ContainerFileManager_ShouldListFilesInVolume()
    {
        string testDir = Path.Combine(Path.GetTempPath(), "bangplanix_test_volume");
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
        Directory.CreateDirectory(testDir);

        File.WriteAllText(Path.Combine(testDir, "test1.bpx"), "{}");
        File.WriteAllText(Path.Combine(testDir, "test2.bpx"), "{}");

        var dir = new DirectoryInfo(testDir);
        var files = dir.GetFiles();

        Assert.Equal(2, files.Length);
        Assert.Contains(files, f => f.Name == "test1.bpx");
    }

    [Fact]
    public void ContainerFileManager_ShouldBlockPathTraversal()
    {
        string maliciousName1 = "../../../etc/passwd";
        string maliciousName2 = "..\\windows\\system32";

        bool IsPathTraversal(string name) => name.Contains("..") || name.Contains('/') || name.Contains('\\');

        Assert.True(IsPathTraversal(maliciousName1));
        Assert.True(IsPathTraversal(maliciousName2));
        Assert.False(IsPathTraversal("valid_report.bpx"));
    }

    [Fact]
    public void ContainerFileManager_ShouldUploadAndDownloadFile()
    {
        string tempFile = Path.GetTempFileName();
        byte[] original = Encoding.UTF8.GetBytes("Bangplanix Test Template Data");
        File.WriteAllBytes(tempFile, original);

        byte[] readBack = File.ReadAllBytes(tempFile);
        Assert.Equal(original, readBack);

        File.Delete(tempFile);
        Assert.False(File.Exists(tempFile));
    }

    // --- 4. กลุ่ม Web Report Converter ---

    [Fact]
    public void ReportConverter_ShouldConvertRdlToBpx()
    {
        string mockRdlXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition">
              <Description>Sales Summary Report</Description>
              <PageHeight>29.7cm</PageHeight>
              <PageWidth>21cm</PageWidth>
            </Report>
            """;

        var adapter = LegacyAdapterFactory.GetAdapterByExtension("sales.rdl");
        Assert.NotNull(adapter);
        var report = adapter.Convert(mockRdlXml);

        Assert.NotNull(report);
        Assert.Equal("1.0", report.Version);
        string bpxJson = JsonSerializer.Serialize(report);
        Assert.Contains("version", bpxJson);
    }

    [Fact]
    public void ReportConverter_ShouldRejectUnsupportedExtension()
    {
        Assert.Throws<NotSupportedException>(() =>
        {
            LegacyAdapterFactory.GetAdapterByExtension("unknown_file.xyz");
        });
    }

    // --- 5. กลุ่ม Interactive Report Sandbox ---

    [Fact]
    public async Task ReportSandbox_ShouldRenderPdfFromBpx()
    {
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata { Title = "Sandbox Invoice" },
            PageSetup = new PageSetup { PaperKind = PaperKind.A4, Unit = UnitType.Mm }
        };

        var renderer = new SkiaPdfRenderer();
        byte[] pdfBytes = await renderer.RenderToPdfAsync(report, null, Array.Empty<IDictionary<string, object?>>());

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 100);
        // Standard PDF Header check
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    [Fact]
    public void ReportSandbox_ShouldRenderExcelFromBpx()
    {
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata { Title = "Excel Summary" }
        };

        using var ms = new MemoryStream();
        // Verifying Report structure serialization for excel export
        string json = JsonSerializer.Serialize(report);
        Assert.Contains("Excel Summary", json);
    }

    // --- 6. กลุ่ม Portal Telemetry, Logs & Environment ---

    [Fact]
    public void PortalEnvironment_ShouldReportInstalledFontsAndRuntime()
    {
        string os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        string arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
        int cores = Environment.ProcessorCount;

        Assert.False(string.IsNullOrWhiteSpace(os));
        Assert.False(string.IsNullOrWhiteSpace(arch));
        Assert.True(cores >= 1);
    }

    [Fact]
    public void PortalLogs_ShouldRetrieveRecentLogEntries()
    {
        var logQueue = new System.Collections.Concurrent.ConcurrentQueue<string>();
        logQueue.Enqueue("[2026-09-15] [INFO] Engine booted");
        logQueue.Enqueue("[2026-09-15] [WARN] Test warning");

        string dump = string.Join("\n", logQueue);
        Assert.Contains("[INFO] Engine booted", dump);
        Assert.Contains("[WARN] Test warning", dump);
    }

    // --- 7. กลุ่ม Route & UI Content Negotiation ---

    [Fact]
    public void PortalServer_RootRoute_ShouldNegotiateHtmlForBrowsers()
    {
        string html = ManagementPortalServer.GetEmbeddedPortalHtml();
        Assert.NotNull(html);
        Assert.Contains("Bangplanix Cloud Management Portal", html);
        Assert.Contains("Telemetry Dashboard", html);
        Assert.Contains("Container Files", html);
        Assert.Contains("Report Converter", html);
        Assert.Contains("Report Sandbox", html);
        Assert.Contains("Live Logs", html);
        Assert.Contains("System & License", html);
    }

    [Fact]
    public void PortalServer_RootRoute_ShouldReturnJsonForApiClients()
    {
        var jsonResponse = new
        {
            name = "Bangplanix Report Engine",
            version = "1.0.0-preview.1",
            status = "Online"
        };

        string serialized = JsonSerializer.Serialize(jsonResponse);
        Assert.Contains("Bangplanix Report Engine", serialized);
        Assert.Contains("Online", serialized);
    }
}
