using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bangplanix.Printing.EscPos;
using Bangplanix.Printing.Protocols;
using Bangplanix.Printing.Spooler;
using Bangplanix.Printing.Zpl;
using FluentAssertions;
using Xunit;

#pragma warning disable CA1707, CA2000, CA2007

namespace Bangplanix.Printing.Tests;

public class PrintingStreamEmulationTests
{
    [Fact]
    public async Task EscPosReceiptStream_ShouldTransmitFullReceiptToEmulatedPrinter()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var receivedBytes = new List<byte>();
        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                var buffer = new byte[1024];
                int read;
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    receivedBytes.AddRange(buffer.Take(read));
                }
            }
            catch (Exception)
            {
                // Ignored on cleanup
            }
        });

        using (var gen = EscPosGenerator.CreateThai())
        {
            gen.Align(EscPosAlignment.Center)
               .Bold(true)
               .TextLine("BANGPLANIX COFFEE")
               .Bold(false)
               .Align(EscPosAlignment.Left)
               .TwoColumns("Latte Grande", "THB 120.00", 32)
               .TwoColumns("Croissant Butter", "THB  85.00", 32)
               .FeedLines(2)
               .Cut(EscPosCutType.Partial);

            var receiptData = gen.ToByteArray();

            var printer = new RawSocketPrinter();
            var success = await printer.PrintAsync(receiptData, "127.0.0.1", port);

            await serverTask;
            listener.Stop();

            success.Should().BeTrue();
            receivedBytes.Should().NotBeEmpty();
            receivedBytes.Should().Contain(0x1B); // ESC
            receivedBytes.Should().Contain(0x1D); // GS (cut command)
        }
    }

    [Fact]
    public void ZplLabelStream_ShouldGenerateValidIndustrialZplPayload()
    {
        var gen = new ZplGenerator(ZplDpi.Dpi203);
        gen.Text(50, 50, "PARCEL DELIVERY", fontHeightDots: 35)
           .Barcode128(50, 120, "TRACK-987654321", height: 80)
           .QrCode(50, 240, "https://bangplanix.io/track/987654321", magnification: 6)
           .EndLabel();

        var zplString = gen.Build();

        zplString.Should().StartWith("^XA");
        zplString.TrimEnd().Should().EndWith("^XZ");
        zplString.Should().Contain("PARCEL DELIVERY");
        zplString.Should().Contain("TRACK-987654321");
    }

    [Fact]
    public async Task SpoolerStream_ShouldDeliverQueuedJobToMockPrinter()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var receivedData = new List<byte>();
        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                var buffer = new byte[512];
                int read;
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    receivedData.AddRange(buffer.Take(read));
                }
            }
            catch (Exception)
            {
                // Ignored on cleanup
            }
        });

        var spooler = new PrintSpoolerQueue();
        var rawPayload = Encoding.UTF8.GetBytes("^XA^FO50,50^FDSpooler Job^FS^XZ");
        var jobId = spooler.Enqueue(rawPayload, "127.0.0.1", port, "tcp", "Emulation Spooler Test");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await spooler.ProcessQueueAsync(cts.Token);

        await serverTask;
        listener.Stop();

        var job = spooler.GetJob(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(PrintJobStatus.Completed);
        receivedData.Should().Equal(rawPayload);
    }
}
