using System.Xml.Serialization;

namespace XMLFactory.Interfaces
{
    public interface IExporterModule
    {
        bool GenerateXmlFile(string xmlFilepath, out IRootXmlClass rootXmlObj);
        XmlSerializerNamespaces GetNamespaces();
    }
}
