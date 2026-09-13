using Bangplanix.Core.Diagnostics;
using Xunit;

namespace Bangplanix.Engine.Tests;

public sealed class CliDoctorDiagnosticsTests
{
    [Fact]
    public void SystemDoctor_RunFullDiagnostics_ShouldReturnComprehensiveReport()
    {
        var tempSpill = Path.Combine(Path.GetTempPath(), $"bangplanix_doctor_test_{Guid.NewGuid():N}");
        try
        {
            var report = SystemDoctor.RunFullDiagnostics(tempSpill);

            Assert.NotNull(report);
            Assert.NotEmpty(report.Checks);
            Assert.True(report.AllCriticalPassed);

            var asciiOutput = report.FormatAsciiReport();
            Assert.Contains("BANGPLANIX ENTERPRISE SYSTEM HEALTH & DOCTOR REPORT", asciiOutput);
            Assert.Contains("RUNTIME & PLATFORM", asciiOutput);
            Assert.Contains("HARDWARE ACCELERATION", asciiOutput);
            Assert.Contains("SECURITY & CRYPTO", asciiOutput);
            Assert.Contains("STORAGE & MEMORY", asciiOutput);
            Assert.Contains("PASS", asciiOutput);
        }
        finally
        {
            if (Directory.Exists(tempSpill))
            {
                try { Directory.Delete(tempSpill, true); } catch { }
            }
        }
    }

    [Fact]
    public void SystemDoctor_ShouldDetectCustomFontDirectory()
    {
        string tempFontDir = Path.Combine(Path.GetTempPath(), $"fonts_doctor_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFontDir);
        try
        {
            // Create dummy ttf file
            string dummyTtf = Path.Combine(tempFontDir, "CustomThaiFont.ttf");
            File.WriteAllBytes(dummyTtf, new byte[] { 0x00, 0x01, 0x00, 0x00 });

            var report = SystemDoctor.RunFullDiagnostics(customFontDir: tempFontDir);
            var fontCheck = report.Checks.FirstOrDefault(c => c.Category == "Typography & Shaping");

            Assert.NotNull(fontCheck);
            Assert.Contains("1 font file", fontCheck.Details);
        }
        finally
        {
            if (Directory.Exists(tempFontDir))
            {
                try { Directory.Delete(tempFontDir, true); } catch { }
            }
        }
    }
}
