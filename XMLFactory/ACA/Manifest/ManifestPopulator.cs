using System;
using System.IO;
using XMLFactory.ACA.FormData;
using XMLFactory.Common;
using XMLFactory.Interfaces;
using FormData_USAddressGrpType = XMLFactory.ACA.FormData.USAddressGrpType;

namespace XMLFactory.ACA.Manifest
{
    internal class ManifestPopulator : IXmlDataPopulator
    {
        /// <summary>
        /// Required by IRS spec as part of the unique transmission identifier.  See IRS publications 5164 and 5165.
        /// </summary>
        private const string SYS12_IRS_CONSTANT = "SYS12";
        /// <summary>
        /// Required by IRS spec as part of the unique transmission identifier.  See IRS publications 5164 and 5165.
        /// </summary>
        private const string TRANSACTIONAL_IRS_CONSTANT = "T";
        private const string TEST_IRS_CONSTANT = "T";
        private const string PRODUCTION_IRS_CONSTANT = "P";

        private readonly TransmissionContext currentContext;
        private static Form109495CTransmittalUpstreamType _formDataRootXmlObj;

        private static Form1094CUpstreamDetailType _pr1094CDetailNd;


        /// <summary>
        /// Set read only instance variables for submitted transmission.
        /// </summary>
        /// <param name="currentContext">Infomration about the transmission taken from the current form state when transmission is exxported.</param>
        /// <param name="formDataRootXmlObj">Form Data Xml object tree generated for the current transmission.</param>
        public ManifestPopulator(TransmissionContext currentContext, Form109495CTransmittalUpstreamType formDataRootXmlObj)
        {
            this.currentContext = currentContext;
            _formDataRootXmlObj = formDataRootXmlObj;
            _pr1094CDetailNd = formDataRootXmlObj.Form1094CUpstreamDetail[0];        
        }

        /// <summary>
        /// Populates object tree of the manifest file.  Values that are not required are nulled out or commented out to suppress the element in the
        /// generated xml.  
        /// </summary>
        /// <returns>TransmistterACAUIBusinessHeader manifestRootXmlObj</returns>
        public IRootXmlClass GetXmlRootClassPopulated()
        {
            var transmitterACAUIBusinessHeader = new TransmitterACAUIBusinessHeaderType
            {
                ACABusinessHeader = new ACABulkBusinessHeaderRequestType
                {
                    UniqueTransmissionId = string.Format("{0}:{1}:{2}:{3}:{4}",
                        currentContext.Uuid,
                        SYS12_IRS_CONSTANT,
                        currentContext.TransmitterControlCode,
                        string.Empty,
                        TRANSACTIONAL_IRS_CONSTANT),

                    Timestamp = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss+00:00"),
                    Id = null
                    //AnyAttr = new List<XmlAttribute>()
                },
                ACATransmitterManifestReqDtl = _PopulateACATrnsmtManifestReqDtl(_formDataRootXmlObj, currentContext)
            };

            return transmitterACAUIBusinessHeader;
        }

        private static ACATrnsmtManifestReqDtlType _PopulateACATrnsmtManifestReqDtl(Form109495CTransmittalUpstreamType formDataRootXmlObject, TransmissionContext transmissionContext)
        {
            var acaTrnsmtManifestReqDtlType = new ACATrnsmtManifestReqDtlType
            {
                PaymentYr                   = transmissionContext.PaymentYear.ToString(),
                PriorYearDataInd            = TransmissionContext.PriorYearDataInd ? DigitBooleanType.Item1 : DigitBooleanType.Item0,
                EIN                         = formDataRootXmlObject.Form1094CUpstreamDetail[0].EmployerInformationGrp.EmployerEIN,
                TransmissionTypeCd          = _getTransmissionType(transmissionContext.TransmissionType),
                OriginalReceiptId           = transmissionContext.TransmissionType == "R" ? transmissionContext.CorrectedReceiptID : null,
                TotalPayerRecordCnt         = "1",
                FormTypeCd                  = FormNameType.Item10941095C,
                BinaryFormatCd              = BinaryFormatCodeType.applicationxml,
                ChecksumAugmentationNum     = Helper.GetMD5(transmissionContext.FormDataFilePath),
                AttachmentByteSizeNum       = Helper.GetFilesize(transmissionContext.FormDataFilePath).ToString(),
                CompanyInformationGrp       = _populateCompanyInformationGrpType(_pr1094CDetailNd),
                DocumentSystemFileNm        = Path.GetFileName(transmissionContext.FormDataFilePath),
                Id                          = null,
                SoftwareId                  = VendorInfo.SoftwareID ?? string.Empty,
                TestFileCd                  = transmissionContext.IsTest ? TEST_IRS_CONSTANT : PRODUCTION_IRS_CONSTANT,
                TotalPayeeRecordCnt         = formDataRootXmlObject.Form1094CUpstreamDetail[0].Form1095CAttachedCnt,
                TransmitterForeignEntityInd = DigitBooleanType.Item0,
                VendorInformationGrp        = _PopulateVendorInformationGrpType(),

                TransmitterNameGrp = new BusinessNameType
                {
                    BusinessNameLine1Txt = _pr1094CDetailNd.EmployerInformationGrp.BusinessName.BusinessNameLine1Txt,
                },
                //AnyAttr = new List<XmlAttribute>()
            };

            return acaTrnsmtManifestReqDtlType;
        }

        private static VendorInformationGrpType _PopulateVendorInformationGrpType()
        {
            var vendorInformationGrpType = new VendorInformationGrpType
            {
                ContactNameGrp =
                {
                    PersonFirstNm  = VendorInfo.VendorPersonFirstName ?? string.Empty,
                    PersonMiddleNm = null,
                    PersonLastNm   = VendorInfo.VendorPersonLastName  ?? string.Empty,
                    SuffixNm       = null
                },

                ContactPhoneNum = VendorInfo.VendorContactPhone ?? string.Empty,
                VendorCd        = VendorInfo.VendorInd          ?? string.Empty                               
            };

            return vendorInformationGrpType;
        }

        private static CompanyInformationGrpType _populateCompanyInformationGrpType(Form1094CUpstreamDetailType pr1094CDetailNd)
        {
            var employerInformationGrp = pr1094CDetailNd.EmployerInformationGrp;
            var companyInformationGrp = new CompanyInformationGrpType
            {
                MailingAddressGrp = { Item = _getUSAddressGrp(employerInformationGrp) },

                ContactNameGrp =
                {
                    PersonFirstNm  = employerInformationGrp.ContactNameGrp.PersonFirstNm  ?? string.Empty,
                    PersonMiddleNm = null,
                    PersonLastNm   = employerInformationGrp.ContactNameGrp.PersonLastNm   ?? string.Empty,
                    SuffixNm       = null
                },

                ContactPhoneNum = employerInformationGrp.ContactPhoneNum ?? string.Empty,
                CompanyNm       = employerInformationGrp.BusinessName.BusinessNameLine1Txt ?? string.Empty
            };

            return companyInformationGrp;
        }
            
        private static  USAddressGrpType _getUSAddressGrp(EmployerInformationGrpType employerInformationGrp)
        {
            var formDataUsAddressGrpType = employerInformationGrp.MailingAddressGrp.Item as FormData_USAddressGrpType;

            var usAddr = new USAddressGrpType
            {
                AddressLine1Txt  = formDataUsAddressGrpType.AddressLine1Txt ?? string.Empty,
                AddressLine2Txt  = null,
                CityNm           = formDataUsAddressGrpType.CityNm ?? string.Empty,
                USStateCd        = (StateType) Enum.Parse(typeof (StateType), formDataUsAddressGrpType.USStateCd.ToString(), true),
                USZIPCd          = formDataUsAddressGrpType.USZIPCd ?? string.Empty,
                USZIPExtensionCd = null
            };

            return usAddr;
        }

        private static TransmissionTypeCdType _getTransmissionType(string value)
        {
            TransmissionTypeCdType result;
            return Enum.TryParse(value, out result) ? result : TransmissionTypeCdType.O;
        }
    }
}
