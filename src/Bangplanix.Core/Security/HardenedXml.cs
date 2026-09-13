using System.Xml;

namespace Bangplanix.Core.Security;

public static class HardenedXml
{
    public static XmlReaderSettings CreateSafeReaderSettings()
    {
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 1024,
            MaxCharactersInDocument = 10_000_000,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            CloseInput = true
        };
    }

    public static XmlDocument LoadSafeXml(string xmlContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlContent);

        var doc = new XmlDocument
        {
            XmlResolver = null
        };

        using var stringReader = new StringReader(xmlContent);
        using var xmlReader = XmlReader.Create(stringReader, CreateSafeReaderSettings());
        doc.Load(xmlReader);
        return doc;
    }

    public static XmlDocument LoadSafeXml(Stream xmlStream)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);

        var doc = new XmlDocument
        {
            XmlResolver = null
        };

        using var xmlReader = XmlReader.Create(xmlStream, CreateSafeReaderSettings());
        doc.Load(xmlReader);
        return doc;
    }
}
