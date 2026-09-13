using Bangplanix.Core.Models;

namespace Bangplanix.Connectors;

public interface IDataConnector
{
    Task<IReadOnlyList<IDictionary<string, object?>>> FetchDataAsync(DatasetDefinition dataset, IDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default);
}
