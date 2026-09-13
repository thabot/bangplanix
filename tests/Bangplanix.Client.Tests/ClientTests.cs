using System.Net;
using System.Text;
using System.Text.Json;
using Bangplanix.Client;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Client.Tests;

public class ClientTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task RenderReportAsync_MissingTemplate_ShouldThrowArgumentException()
    {
        using var client = new BangplanixClient("http://localhost:9545");
        var act = async () => await client.RenderReportAsync(new RenderClientRequest());
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*Either TemplatePath or TemplateJson must be provided*");
    }

    [Fact]
    public async Task RenderReportAsync_SuccessPdf_ShouldReturnRenderResultWithTelemetry()
    {
        var mockPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4 Mock PDF Content");
        string? capturedCorrelationId = null;

        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/v1/report/render");
            req.Method.Should().Be(HttpMethod.Post);
            capturedCorrelationId = req.Headers.GetValues("X-Correlation-ID").FirstOrDefault();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(mockPdfBytes)
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf") }
                }
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new BangplanixClient("http://localhost:9545", httpClient);

        var result = await client.RenderReportAsync(new RenderClientRequest
        {
            TemplatePath = "schema/v1/samples/invoice.bpx",
            Format = "pdf",
            CorrelationId = "test-corr-001"
        });

        result.Should().NotBeNull();
        result.Format.Should().Be("pdf");
        result.ContentType.Should().Be("application/pdf");
        result.Length.Should().Be(mockPdfBytes.Length);
        result.CorrelationId.Should().Be("test-corr-001");
        capturedCorrelationId.Should().Be("test-corr-001");
        result.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RenderReportAsync_RetryOnTransient503_ShouldSucceed()
    {
        var mockPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4 Recovered PDF");
        int callCount = 0;

        var handler = new MockHttpMessageHandler(req =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("Server temporarily busy")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(mockPdfBytes)
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf") }
                }
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new BangplanixClient("http://localhost:9545", httpClient, maxRetries: 2, retryDelay: TimeSpan.FromMilliseconds(10));

        var result = await client.RenderReportAsync(new RenderClientRequest
        {
            TemplatePath = "schema/v1/samples/invoice.bpx",
            Format = "pdf"
        });

        callCount.Should().Be(2, "Should have retried once after 503");
        result.Length.Should().Be(mockPdfBytes.Length);
    }

    [Fact]
    public async Task RenderToFileAsync_ShouldSaveFileToDisk()
    {
        var mockPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4 File Output");
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(mockPdfBytes)
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf") }
            }
        });

        using var httpClient = new HttpClient(handler);
        using var client = new BangplanixClient("http://localhost:9545", httpClient);

        var tempPath = Path.Combine(Path.GetTempPath(), "bangplanix_test_file.pdf");
        try
        {
            var res = await client.RenderToFileAsync(new RenderClientRequest { TemplatePath = "inv.bpx" }, tempPath);
            File.Exists(tempPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(tempPath)).Should().BeEquivalentTo(mockPdfBytes);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task RenderBatchAsync_ShouldExecuteAllRequestsConcurrently()
    {
        var mockPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4 Batch item");
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(mockPdfBytes)
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf") }
            }
        });

        using var httpClient = new HttpClient(handler);
        using var client = new BangplanixClient("http://localhost:9545", httpClient);

        var requests = Enumerable.Range(1, 5).Select(i => new RenderClientRequest { TemplatePath = $"template_{i}.bpx" });
        var batchResults = await client.RenderBatchAsync(requests, maxDegreeOfParallelism: 3);

        batchResults.Should().HaveCount(5);
        batchResults.Should().OnlyContain(r => r.IsSuccess);
    }

    [Fact]
    public async Task ValidateTemplateAsync_ValidTemplate_ShouldReturnTrue()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/v1/template/validate");
            var json = JsonSerializer.Serialize(new { isValid = true, title = "Commercial Tax Invoice", version = "1.0", errors = Array.Empty<string>() });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new BangplanixClient("http://localhost:9545", httpClient);

        var result = await client.ValidateTemplateAsync("schema/v1/samples/invoice.bpx");

        result.IsValid.Should().BeTrue();
        result.Title.Should().Be("Commercial Tax Invoice");
        result.Version.Should().Be("1.0");
    }

    [Fact]
    public async Task IsHealthyAsync_HealthyServer_ShouldReturnTrue()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/health");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var httpClient = new HttpClient(handler);
        using var client = new BangplanixClient("http://localhost:9545", httpClient);

        var isHealthy = await client.IsHealthyAsync();
        isHealthy.Should().BeTrue();
    }
}