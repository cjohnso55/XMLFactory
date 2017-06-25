using System.Xml.Serialization;
using XMLFactory.ACA.FormData;
using XMLFactory.Interfaces;
using XMLFactory.Common;

namespace XMLFactory.ACA.Manifest
{
    public class ManifestExporter : IExporterModule
    {
        private readonly TransmissionContext _currentContext;
        private readonly Form109495CTransmittalUpstreamType _formDataRootXmlObj;

        /// <summary>
        /// Sets readonly TransmissionContext and IRootXmlClass instance variables.
        /// </summary>
        /// <param name="currentContext">Contains form instance variables/properties for the current context.</param>
        /// <param name="formDataRootXmlObj">A refernce to the form data xml object tree.</param>
        public ManifestExporter(TransmissionContext currentContext, IRootXmlClass formDataRootXmlObj)
        {
            _currentContext         = currentContext;
            _formDataRootXmlObj     = formDataRootXmlObj as Form109495CTransmittalUpstreamType;
        }

        /// <summary>
        /// Populates manifest xml rootXmlObject object tree from the database via a ManifestPopulater instance, and serializes
        /// the xml to disk in the location specified in the xmlFilePath parameter. 
        /// </summary>
        /// <param name="manifestXmlFilepath">Fully qualified path where the manifist XML will be written to disk.</param>
        /// <param name="manifestFile">Output.  A refernce to the manifest xml object tree.</param>
        /// <returns></returns>
        public bool GenerateXmlFile(string manifestXmlFilepath, out IRootXmlClass manifestFile)
        {   
            //Populate in memory.
            var manifestPopulator = new ManifestPopulator(_currentContext, _formDataRootXmlObj);
            manifestFile = manifestPopulator.GetXmlRootClassPopulated();
          
            //Write to xml to disk.
            return Helper.WriteXmlFileToDisk(manifestFile, this.GetNamespaces(), manifestXmlFilepath);
        }

        public XmlSerializerNamespaces GetNamespaces() { return Helper.GetNamespaces(); }
    }
}
