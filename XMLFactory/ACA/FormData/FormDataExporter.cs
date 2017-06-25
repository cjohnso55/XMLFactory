using System.Data;
using System.Xml.Serialization;
using XMLFactory.Common;
using XMLFactory.Interfaces;

namespace XMLFactory.ACA.FormData
{
    /// <summary>
    /// This class moves data from the database to memory using a FormDataPopulator object.  Then writes the corresponding XML to disk
    /// by calling XmlFileCreator.WriteXmlFileToDisk.
    /// </summary>
    public class FormDataExporter : IExporterModule
    {
        private readonly DataTable _employeesToTransmit;
        private readonly DataTable _dependentsToTransmit;
        private readonly DataTable _employerSubsidiariesToTransmit;

        private readonly DataRow _employerInfoToTransmit;
        private readonly DataRow _correctedTransmissionInfo;

        private readonly bool _isTest;
        private readonly bool _generateXmlOnSchemaError;

        public FormDataExporter(DataSet transmissionDataSet, bool isTest, bool generateXmlOnSchemaError)
        {
            _isTest = isTest;
            _generateXmlOnSchemaError = generateXmlOnSchemaError;

            _employeesToTransmit  = transmissionDataSet.Tables[1];
            _dependentsToTransmit = transmissionDataSet.Tables[2];
            _employerSubsidiariesToTransmit = transmissionDataSet.Tables[3];

            _employerInfoToTransmit    = transmissionDataSet.Tables[0].Rows.Count > 0 ? transmissionDataSet.Tables[0].Rows[0] : transmissionDataSet.Tables[0].NewRow();
            _correctedTransmissionInfo = transmissionDataSet.Tables[4].Rows.Count > 0 ? transmissionDataSet.Tables[4].Rows[0] : transmissionDataSet.Tables[4].NewRow();
        }

        public bool GenerateXmlFile(string xmlFilepath, out IRootXmlClass formDataFile)
        {
            var formDataPopulator = new FormDataPopulator(
                    pr1094HeaderRow:               _employerInfoToTransmit, 
                    pr1095HeaderRows:              _employeesToTransmit, 
                    pr1095CoveredRows:             _dependentsToTransmit, 
                    pr1094OtherAleMembers:         _employerSubsidiariesToTransmit,
                    prACAEFileTransmissionHistory: _correctedTransmissionInfo,
                    isTest:                        _isTest,
                    generateXmlOnSchemaError:      _generateXmlOnSchemaError);

            formDataFile = formDataPopulator.GetXmlRootClassPopulated();
          
            return Helper.WriteXmlFileToDisk(formDataFile, this.GetNamespaces(), xmlFilepath);
        }

        public XmlSerializerNamespaces GetNamespaces()
        {
            return Helper.GetNamespaces();
        }
    }

}
