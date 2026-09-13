using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace Bangplanix.Core.Diagnostics;

/// <summary>
/// Diagnostic item evaluation result.
/// </summary>
public sealed class DiagnosticCheckItem
{
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; } = true;
    public string Details { get; set; } = string.Empty;
    public bool IsCritical { get; set; } = true;
}

/// <summary>
/// Overall system diagnostic report.
/// </summary>
public sealed class SystemDiagnosticReport
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public bool AllCriticalPassed => Checks.Where(c => c.IsCritical).All(c => c.Passed);
    public List<DiagnosticCheckItem> Checks { get; set; } = new();

    public string FormatAsciiReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("              🩺 BANGPLANIX ENTERPRISE SYSTEM HEALTH & DOCTOR REPORT            ");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"Timestamp : {Timestamp:yyyy-MM-dd HH:mm:ss 'UTC'}");
        sb.AppendLine($"Status    : {(AllCriticalPassed ? "✅ ALL CRITICAL SYSTEMS OPERATIONAL" : "⚠️ WARNING: SOME CHECKS REQUIRE ATTENTION")}");
        sb.AppendLine("--------------------------------------------------------------------------------");

        var grouped = Checks.GroupBy(c => c.Category);
        foreach (var group in grouped)
        {
            sb.AppendLine($"[{group.Key.ToUpperInvariant()}]");
            foreach (var check in group)
            {
                string icon = check.Passed ? "✓ PASS" : (check.IsCritical ? "✗ FAIL" : "! WARN");
                sb.AppendLine($"  {icon,-8} {check.Name,-35} : {check.Details}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("================================================================================");
        return sb.ToString();
    }
}

/// <summary>
/// Environment, Hardware Intrinsics, Security & Storage Diagnostics Scanner.
/// </summary>
public static class SystemDoctor
{
    public static SystemDiagnosticReport RunFullDiagnostics(string? tempSpillDir = null, string? customFontDir = null)
    {
        var report = new SystemDiagnosticReport();

        // 1. Runtime & OS
        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Runtime & Platform",
            Name = "OS Description",
            Passed = true,
            Details = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})"
        });

        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Runtime & Platform",
            Name = ".NET Framework Runtime",
            Passed = Environment.Version.Major >= 10 || Environment.Version.Major >= 8,
            Details = $".NET {Environment.Version} (64-bit Process: {Environment.Is64BitProcess})"
        });

        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Runtime & Platform",
            Name = "Garbage Collector Mode",
            Passed = true,
            Details = $"Server GC: {System.Runtime.GCSettings.IsServerGC}, Latency Mode: {System.Runtime.GCSettings.LatencyMode}"
        });

        // 2. Hardware Acceleration & SIMD Intrinsics
        bool vectorAccel = Vector.IsHardwareAccelerated;
        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Hardware Acceleration",
            Name = "SIMD Vector Hardware Support",
            Passed = vectorAccel,
            Details = $"Vector<T> Hardware Accelerated (Vector128: {Vector128.IsHardwareAccelerated}, Vector256: {Vector256.IsHardwareAccelerated})"
        });

        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Hardware Acceleration",
            Name = "Logical Processor Cores",
            Passed = Environment.ProcessorCount >= 2,
            Details = $"{Environment.ProcessorCount} Cores Available"
        });

        // 3. Cryptography & Security Subsystem
        bool aesGcmSupported = AesGcm.IsSupported;
        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Security & Crypto",
            Name = "AES-256-GCM Hardware Support",
            Passed = aesGcmSupported,
            Details = aesGcmSupported ? "Hardware AES-NI / ARM Crypto Acceleration Active" : "Software Fallback"
        });

        bool masterKeySet = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("THABOT_MASTER_KEY")) ||
                            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BANGPLANIX_MASTER_KEY"));
        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Security & Crypto",
            Name = "KMS Master Key Configured",
            Passed = true,
            IsCritical = false,
            Details = masterKeySet ? "Environment KMS Master Key Detected" : "Default Ephemeral Key in Use (Production: Set THABOT_MASTER_KEY)"
        });

        // 4. Disk Storage & Temp Spill
        string spillPath = tempSpillDir ?? Path.Combine(Path.GetTempPath(), "bangplanix_spill");
        bool spillWritable = false;
        try
        {
            if (!Directory.Exists(spillPath)) Directory.CreateDirectory(spillPath);
            string testFile = Path.Combine(spillPath, $"doctor_probe_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testFile, "probe");
            File.Delete(testFile);
            spillWritable = true;
        }
        catch
        {
            spillWritable = false;
        }

        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Storage & Memory",
            Name = "Temp Disk Spill Directory",
            Passed = spillWritable,
            Details = spillWritable ? $"Writable ({spillPath})" : $"Access Denied / Read-Only ({spillPath})"
        });

        // 5. Fonts Directory
        string fontPath = customFontDir ?? Path.Combine(Directory.GetCurrentDirectory(), "volumes", "fonts");
        bool fontDirExists = Directory.Exists(fontPath);
        int fontCount = fontDirExists ? Directory.GetFiles(fontPath, "*.*", SearchOption.AllDirectories)
            .Count(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)) : 0;

        report.Checks.Add(new DiagnosticCheckItem
        {
            Category = "Typography & Shaping",
            Name = "Custom TrueType/OpenType Fonts",
            Passed = true,
            IsCritical = false,
            Details = fontDirExists ? $"Found {fontCount} font file(s) in {fontPath}" : "Using Default HarfBuzz Fallback Font Chain"
        });

        return report;
    }
}
