using System.Net;
using System.Net.Sockets;
using Bangplanix.Printing.Protocols;
using Bangplanix.Printing.Spooler;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Printing.Tests;

public class ProtocolAndSpoolerTests
{
    [Fact]
    public async Task RawSocketPrinter_ShouldSendBytesToTcpListener()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
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
                // Ignored on test cleanup
            }
        });

        var printer = new RawSocketPrinter();
        var payload = new byte[] { 0x1B, 0x40, 0x41, 0x42, 0x43, 0x0A };
        var success = await printer.PrintAsync(payload, "127.0.0.1", port);

        await serverTask;
        listener.Stop();

        success.Should().BeTrue();
        receivedBytes.Should().Equal(payload);
    }

    [Fact]
    public void IppPrinter_BuildIppPrintJobRequest_ShouldContainValidIppHeaders()
    {
        var doc = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var req = IppPrinter.BuildIppPrintJobRequest("http://localhost:631/ipp/print", doc);

        req.Should().NotBeEmpty();
        req[0].Should().Be(0x02); // IPP 2.0
        req[1].Should().Be(0x00);
        req[2].Should().Be(0x00); // Operation Print-Job (0x0002)
        req[3].Should().Be(0x02);
    }

    [Fact]
    public void PrintSpoolerQueue_EnqueueAndTrackHistory_ShouldManageJobs()
    {
        var spooler = new PrintSpoolerQueue();
        var jobId = spooler.Enqueue(new byte[] { 0x1B, 0x40 }, "127.0.0.1", 9100, "tcp", "Invoice Receipt");

        jobId.Should().NotBeNullOrWhiteSpace();

        var job = spooler.GetJob(jobId);
        job.Should().NotBeNull();
        job!.DocumentName.Should().Be("Invoice Receipt");
        job.Status.Should().Be(PrintJobStatus.Queued);

        var all = spooler.GetAllJobs();
        all.Should().ContainSingle(j => j.JobId == jobId);
    }

    [Fact]
    public async Task PrintSpoolerQueue_ProcessQueue_WhenServerOffline_ShouldRetryAndFailGracefully()
    {
        var spooler = new PrintSpoolerQueue();
        // Use an unassigned port on loopback
        var jobId = spooler.Enqueue(new byte[] { 0x1B, 0x40 }, "127.0.0.1", 59999, "tcp", "Test Fail", maxRetries: 1);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await spooler.ProcessQueueAsync(cts.Token);

        var job = spooler.GetJob(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be(PrintJobStatus.Failed);
        job.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
