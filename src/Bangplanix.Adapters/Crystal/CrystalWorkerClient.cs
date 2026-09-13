using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;

namespace Bangplanix.Adapters.Crystal;

public sealed class CrystalWorkerClient
{
    private readonly HttpClient _httpClient;
    private readonly string _workerBaseUrl;

    public CrystalWorkerClient(HttpClient? httpClient = null, string? workerBaseUrl = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _workerBaseUrl = (workerBaseUrl ?? Environment.GetEnvironmentVariable("CRYSTAL_WORKER_URL") ?? "http://localhost:8080").TrimEnd('/');
    }

    public string WorkerBaseUrl => _workerBaseUrl;

    public bool IsWorkerConfigured => !string.IsNullOrWhiteSpace(_workerBaseUrl);

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_workerBaseUrl}/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ReportDefinition> ConvertRptStreamAsync(
        Stream rptStream,
        string fileName = "report.rpt",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rptStream);

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(rptStream);
        content.Add(streamContent, "file", fileName);

        var url = $"{_workerBaseUrl}/convert";
        var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string errBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"Crystal Reports Micro-Worker returned status {(int)response.StatusCode} ({response.ReasonPhrase}): {errBody}");
        }

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await BpxParser.ParseAsync(responseStream, cancellationToken).ConfigureAwait(false);
    }

    public ReportDefinition ConvertRptStream(Stream rptStream, string fileName = "report.rpt")
    {
        return ConvertRptStreamAsync(rptStream, fileName).GetAwaiter().GetResult();
    }
}
