using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Common;

public interface ILegacyReportAdapter
{
    string FormatName { get; }
    IReadOnlyList<string> SupportedExtensions { get; }
    ReportDefinition Convert(string content);
    ReportDefinition Convert(Stream stream);
}
