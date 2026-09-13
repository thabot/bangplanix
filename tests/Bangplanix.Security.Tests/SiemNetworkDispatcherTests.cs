using System.Net;
using Bangplanix.Core.Audit;
using Bangplanix.Engine.Security;
using Xunit;

namespace Bangplanix.Security.Tests;

internal sealed class SiemMockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
    public int RequestCount { get; private set; }
    public HttpRequestMessage? LastRequest { get; private set; }

    public SiemMockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequest = request;
        return Task.FromResult(_handler(request));
    }
}

public class SiemNetworkDispatcherTests
{
    [Fact]
    public async Task SiemNetworkDispatcher_ShouldQueueAndFlushBatchSuccessfully()
    {
        var mockHttp = new SiemMockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(mockHttp);

        var config = new SiemEndpointConfig
        {
            EndpointUri = new Uri("https://siem.enterprise.corp/api/v1/events"),
            DestinationType = SiemDestinationType.SplunkHec,
            AuthToken = "splunk-hec-token-secret-12345",
            MaxBatchSize = 10,
            FlushInterval = TimeSpan.FromMilliseconds(500)
        };

        await using var dispatcher = new SiemNetworkDispatcher(config, client);

        for (int i = 1; i <= 5; i++)
        {
            var ev = new AuditEvent
            {
                EventType = AuditEventType.ReportRendered,
                Action = $"Render_{i}",
                ResourceName = $"Report_{i}.bpx"
            };
            await dispatcher.EmitAsync(ev);
        }

        Assert.Equal(5, dispatcher.QueuedEventsCount);

        var flushed = await dispatcher.FlushBatchAsync();
        Assert.Equal(5, flushed);
        Assert.Equal(5, dispatcher.TotalDispatchedEvents);
        Assert.Equal(0, dispatcher.FailedDispatches);
        Assert.Equal(1, mockHttp.RequestCount);

        // Verify Splunk Auth Header
        Assert.NotNull(mockHttp.LastRequest);
        Assert.Equal("Splunk", mockHttp.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("splunk-hec-token-secret-12345", mockHttp.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SiemNetworkDispatcher_ShouldRetryOnTransientError()
    {
        int attempts = 0;
        var mockHttp = new SiemMockHttpMessageHandler(req =>
        {
            attempts++;
            return attempts < 2 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = new HttpClient(mockHttp);

        var config = new SiemEndpointConfig
        {
            EndpointUri = new Uri("https://datadog.corp/api/v2/logs"),
            DestinationType = SiemDestinationType.DatadogIntake,
            AuthToken = "dd-api-key-999",
            MaxBatchSize = 10,
            MaxRetries = 3
        };

        await using var dispatcher = new SiemNetworkDispatcher(config, client);
        await dispatcher.EmitAsync(new AuditEvent { EventType = AuditEventType.DataExported });

        var flushed = await dispatcher.FlushBatchAsync();
        Assert.Equal(1, flushed);
        Assert.Equal(1, dispatcher.TotalDispatchedEvents);
        Assert.Equal(2, mockHttp.RequestCount);
    }
}
