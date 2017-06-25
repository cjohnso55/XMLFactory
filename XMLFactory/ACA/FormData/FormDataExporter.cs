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
        private readonly DataTable employeesToTransmit;
        private readonly DataTable dependentsToTransmit;
        private readonly DataTable employerSubsidiariesToTransmit;

        private readonly DataRow employerInfoToTransmit;
        private readonly DataRow correctedTransmissionInfo;

        private readonly bool isTest;
        private readonly bool generateXmlOnSchemaError;

        public FormDataExporter(DataSet transmissionDataSet, bool isTest, bool generateXmlOnSchemaError)
        {
            isTest = this.isTest;
            generateXmlOnSchemaError = this.generateXmlOnSchemaError;

            employeesToTransmit  = transmissionDataSet.Tables[1];
            dependentsToTransmit = transmissionDataSet.Tables[2];
            employerSubsidiariesToTransmit = transmissionDataSet.Tables[3];

            employerInfoToTransmit    = transmissionDataSet.Tables[0].Rows.Count > 0 ? transmissionDataSet.Tables[0].Rows[0] : transmissionDataSet.Tables[0].NewRow();
            correctedTransmissionInfo = transmissionDataSet.Tables[4].Rows.Count > 0 ? transmissionDataSet.Tables[4].Rows[0] : transmissionDataSet.Tables[4].NewRow();
        }

        public bool GenerateXmlFile(string xmlFilepath, out IRootXmlClass formDataFile)
        {
            var formDataPopulator = new FormDataPopulator(
                    pr1094HeaderRow:               employerInfoToTransmit, 
                    pr1095HeaderRows:              employeesToTransmit, 
                    pr1095CoveredRows:             dependentsToTransmit, 
                    pr1094OtherAleMembers:         employerSubsidiariesToTransmit,
                    prACAEFileTransmissionHistory: correctedTransmissionInfo,
                    isTest:                        isTest,
                    generateXmlOnSchemaError:      generateXmlOnSchemaError);

            formDataFile = formDataPopulator.GetXmlRootClassPopulated();
          
            return Helper.WriteXmlFileToDisk(formDataFile, this.GetNamespaces(), xmlFilepath);
        }

        public XmlSerializerNamespaces GetNamespaces()
        {
            return Helper.GetNamespaces();
        }
    }

}
