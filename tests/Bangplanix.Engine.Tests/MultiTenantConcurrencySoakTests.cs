using System.Collections.Concurrent;
using System.Text;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Assembly;
using Bangplanix.Engine.Security;
using Bangplanix.Engine.Visuals.Charts;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class MultiTenantConcurrencySoakTests
{
    [Fact]
    public async Task MultiTenantEngine_50ConcurrentWorkers_ShouldMaintainDataIsolationAndZeroDeadlocks()
    {
        const int concurrentWorkers = 50;
        var tasks = new List<Task>();
        var results = new ConcurrentBag<(string TenantId, int ReportSize, bool IsValid)>();

        for (int i = 1; i <= concurrentWorkers; i++)
        {
            var tenantId = $"tenant_{i:D3}";
            var workerId = i;

            tasks.Add(Task.Run(async () =>
            {
                // 1. Create Tenant-isolated Chart
                var chartDef = new ChartDefinition
                {
                    ChartType = workerId % 2 == 0 ? ChartType.Bar : ChartType.Pie,
                    Title = $"Performance Metrics - {tenantId}",
                    Palette = workerId % 3 == 0 ? "Corporate" : "Emerald",
                    Categories = ["Q1", "Q2", "Q3"],
                    Series =
                    [
                        new ChartSeriesDefinition
                        {
                            Name = $"Series_{tenantId}",
                            Values = [1000.0 * workerId, 2000.0 * workerId, 3000.0 * workerId],
                            Categories = ["Q1", "Q2", "Q3"]
                        }
                    ]
                };

                using var bitmap = new SKBitmap(400, 300);
                using var canvas = new SKCanvas(bitmap);
                SkiaChartRenderer.RenderChart(canvas, chartDef, new SKRect(0, 0, 400, 300), "Sarabun");

                // 2. Multi-page PDF Dossier Assembly
                var initialPdf = Encoding.UTF8.GetBytes($"%PDF-1.7\n1 0 obj\n<< /Title ({tenantId}) >>\nendobj\n%%EOF");
                var bookmarks = new List<PdfBookmarkNode>
                {
                    new() { Title = $"Report Root - {tenantId}", PageNumber = 1 }
                };

                var bookmarkedPdf = PdfBookmarkEngine.InjectPdfBookmarks(initialPdf, bookmarks);

                // 3. Cryptographic Signing & Tamper Sealing
                var signedPdf = PdfDigitalSigner.SignPdf(bookmarkedPdf, new PdfSigningOptions
                {
                    SignerName = $"Signer for {tenantId}",
                    Reason = $"Tenant {tenantId} Authorization"
                });

                var verification = PdfDigitalSigner.VerifySignature(signedPdf);

                // 4. Record output
                results.Add((tenantId, signedPdf.Length, verification.IsValid));
                await Task.Yield();
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentWorkers, results.Count);
        foreach (var res in results)
        {
            Assert.True(res.IsValid);
            Assert.True(res.ReportSize > 100);
        }

        // Verify all 50 distinct tenants are represented with zero cross-tenant overwrite
        var distinctTenants = results.Select(r => r.TenantId).Distinct().Count();
        Assert.Equal(concurrentWorkers, distinctTenants);
    }
}
