using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bangplanix.Adapters.Common;
using Bangplanix.Adapters.Crystal;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class CrystalWorkerClientTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
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
    public async Task CheckHealthAsync_ShouldReturnTrue_WhenWorkerReturns200()
    {
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/health");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
            };
        });

        var client = new CrystalWorkerClient(new HttpClient(mockHandler), "http://test-worker:8080");
        var isHealthy = await client.CheckHealthAsync();

        isHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnFalse_WhenWorkerReturns500OrUnreachable()
    {
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        var client = new CrystalWorkerClient(new HttpClient(mockHandler), "http://test-worker:8080");
        var isHealthy = await client.CheckHealthAsync();

        isHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnFalse_WhenHttpRequestThrowsException()
    {
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            throw new HttpRequestException("Connection refused");
        });

        var client = new CrystalWorkerClient(new HttpClient(mockHandler), "http://offline-worker:8080");
        var isHealthy = await client.CheckHealthAsync();

        isHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task ConvertRptStreamAsync_ShouldReturnReportDefinition_WhenWorkerReturnsJson()
    {
        var expectedReport = new ReportDefinition
        {
            Metadata = new ReportMetadata
            {
                Title = "Converted Sales Report",
                Author = "Crystal Worker Engine"
            },
            PageSetup = new PageSetup
            {
                Width = 595.28,
                Height = 841.89,
                Orientation = PageOrientation.Portrait
            }
        };

        string jsonPayload = BpxParser.ToJson(expectedReport);

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.PathAndQuery.Should().Be("/convert");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
        });

        var client = new CrystalWorkerClient(new HttpClient(mockHandler), "http://test-worker:8080");

        using var sampleStream = new MemoryStream(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0x00, 0x01, 0x02, 0x03 });
        var report = await client.ConvertRptStreamAsync(sampleStream, "sales.rpt");

        report.Should().NotBeNull();
        report.Metadata.Title.Should().Be("Converted Sales Report");
        report.Metadata.Author.Should().Be("Crystal Worker Engine");
        report.PageSetup.Width.Should().BeApproximately(595.28, 0.01);
    }

    [Fact]
    public async Task ConvertRptStreamAsync_ShouldThrowInvalidOperationException_WhenWorkerReturns500()
    {
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Failed to decrypt binary section", Encoding.UTF8, "text/plain")
            };
        });

        var client = new CrystalWorkerClient(new HttpClient(mockHandler), "http://test-worker:8080");

        using var sampleStream = new MemoryStream(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 });
        Func<Task> act = async () => await client.ConvertRptStreamAsync(sampleStream, "corrupted.rpt");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Crystal Reports Micro-Worker returned status 500*Failed to decrypt binary section*");
    }

    [Fact]
    public async Task ConvertRptStreamAsync_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        var client = new CrystalWorkerClient(new HttpClient(new MockHttpMessageHandler(_ => new HttpResponseMessage())), "http://test-worker:8080");
        Func<Task> act = async () => await client.ConvertRptStreamAsync(null!, "null.rpt");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void CrystalReportsXmlAdapter_ShouldConvertDirectly_WhenStreamIsXmlText()
    {
        string sampleXml = @"<CrystalReport Title=""Native Direct XML"">
  <Section Kind=""DetailSection"" Height=""300"">
    <TextObject Name=""txt1"" Left=""100"" Top=""50"" Width=""500"" Height=""100"">
      <Text>Hello Crystal XML</Text>
    </TextObject>
  </Section>
</CrystalReport>";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sampleXml));
        var adapter = new CrystalReportsXmlAdapter();
        var report = adapter.Convert(stream);

        report.Should().NotBeNull();
        report.Metadata.Title.Should().Be("Native Direct XML");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Text.Should().Be("Hello Crystal XML");
    }

    [Fact]
    public void LegacyAdapterFactory_RegisteredAdapters_ShouldContainAll21StandardAdapters()
    {
        var adapters = LegacyAdapterFactory.RegisteredAdapters;
        adapters.Should().HaveCount(21);

        // Verify .rpt and .rpt.xml resolution
        var rptXmlAdapter = LegacyAdapterFactory.GetAdapterByExtension("invoice.rpt.xml");
        rptXmlAdapter.Should().BeOfType<CrystalReportsXmlAdapter>();

        var rptAdapter = LegacyAdapterFactory.GetAdapterByExtension("invoice.rpt");
        rptAdapter.Should().BeOfType<CrystalReportsXmlAdapter>();
    }
}
