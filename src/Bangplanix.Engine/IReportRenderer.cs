using Bangplanix.Core.Models;

namespace Bangplanix.Engine;

public interface IReportRenderer
{
    Task<byte[]> RenderToPdfAsync(ReportDefinition report, IDictionary<string, object?>? parameters = null, IReadOnlyList<IDictionary<string, object?>>? mainDataRows = null, CancellationToken cancellationToken = default);
    Task RenderToStreamAsync(ReportDefinition report, Stream outputStream, IDictionary<string, object?>? parameters = null, IReadOnlyList<IDictionary<string, object?>>? mainDataRows = null, CancellationToken cancellationToken = default);
}

