using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bangplanix.Connectors;
using Bangplanix.Connectors.Sap;
using Bangplanix.Core.Models;
using Xunit;

namespace Bangplanix.Connectors.Tests;

public class SapRaylightConnectorTests
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
    public void ParseRaylightJson_ShouldParseFlowStructure()
    {
        const string json = """
            {
              "flow": {
                "columns": ["Article", "Quantity", "Revenue"],
                "rows": [
                  ["Widget A", 10, 1500.50],
                  ["Widget B", 5, 750.00]
                ]
              }
            }
            """;

        var rows = SapRaylightConnector.ParseRaylightJson(Encoding.UTF8.GetBytes(json));

        Assert.Equal(2, rows.Count);
        Assert.Equal("Widget A", rows[0]["Article"]);
        Assert.Equal(10L, Convert.ToInt64(rows[0]["Quantity"]));
        Assert.Equal(1500.50, Convert.ToDouble(rows[0]["Revenue"]));

        Assert.Equal("Widget B", rows[1]["Article"]);
        Assert.Equal(5L, Convert.ToInt64(rows[1]["Quantity"]));
    }

    [Fact]
    public void ParseRaylightXml_ShouldParseXmlStructure()
    {
        const string xml = """
            <flow xmlns="http://www.sap.com/rws/bip">
              <columns>
                <column name="CustomerID" />
                <column name="CustomerName" />
              </columns>
              <rows>
                <row>
                  <value name="CustomerID">CUST-001</value>
                  <value name="CustomerName">Bangplanix Corp</value>
                </row>
                <row>
                  <value name="CustomerID">CUST-002</value>
                  <value name="CustomerName">Enterprise Ltd</value>
                </row>
              </rows>
            </flow>
            """;

        var rows = SapRaylightConnector.ParseRaylightXml(xml);

        Assert.Equal(2, rows.Count);
        Assert.Equal("CUST-001", rows[0]["CustomerID"]);
        Assert.Equal("Bangplanix Corp", rows[0]["CustomerName"]);
    }

    [Fact]
    public async Task FetchDataAsync_ShouldPassLogonTokenAndReturnRows()
    {
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.True(req.Headers.Contains("X-SAP-LogonToken"));
            Assert.Contains("valid-secret-token", req.Headers.GetValues("X-SAP-LogonToken"));

            const string responseJson = """
                {
                  "flow": {
                    "columns": ["Id", "Status"],
                    "rows": [
                      [101, "Active"],
                      [102, "Pending"]
                    ]
                  }
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var connector = new SapRaylightConnector(
            serverBaseUrl: "https://1.1.1.1/biprws",
            logonToken: "valid-secret-token",
            httpHandler: mockHandler);

        var dataset = new DatasetDefinition
        {
            Name = "SapDataProvider",
            Type = DatasetType.SapRaylight,
            QueryOrUrl = "https://1.1.1.1/biprws/raylight/v1/documents/12345/dataProviders/DP0/flows/0"
        };

        var data = await connector.FetchDataAsync(dataset);

        Assert.Equal(2, data.Count);
        Assert.Equal(101L, Convert.ToInt64(data[0]["Id"]));
        Assert.Equal("Active", data[0]["Status"]);
    }

    [Fact]
    public void DataConnectorFactory_ShouldCreateSapRaylightConnector()
    {
        var connector = DataConnectorFactory.CreateConnector(DatasetType.SapRaylight, bearerToken: "test-token");
        Assert.NotNull(connector);
        Assert.IsType<SapRaylightConnector>(connector);
    }
}
