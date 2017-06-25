using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using XMLFactory.Interfaces;
using XMLFactory.Common;


namespace XMLFactory.ACA.FormData
{
    public class FormDataPopulator : IXmlDataPopulator
    {
        /// <summary>
        /// Stipulated as a requirement by the IRS.  It is required by the COTS softare being used to digest the XML.
        /// </summary>
        private const string LINE_NUMBER_IRS_CONSTANT = "0";
        /// <summary>
        /// Stipulated as a requirement by the IRS.  It is required by the COTS softare being used to digest the XML.
        /// </summary>
        private const string RECORD_TYPE_IRS_CONSTANT = "String";
        /// <summary>
        /// There will only be one submission (1094C/Employer) record per transmission since we limited it to a single company.  
        /// </summary>
        private const string SUBMISSION_ID = "1";
        /// <summary>
        /// Illegal characters and their associated escape sequences.
        /// </summary>
        const string ESCAPE_NAME_CONTROL_PROCESSING = @"\~";

        private readonly DataRow   _pr1094HeaderRow;
        private readonly DataTable _pr1095HeaderRows;
        private readonly DataTable _pr1095CoveredRows;
        private readonly DataTable _pr1094OtherMemberRows;

        private readonly int    _taxYear;
        private readonly int    _transmittedEmployeeCount;
        private readonly char   _transmittalType;
        private readonly string _correctedReceiptID;

        private readonly bool _isTest;
        private readonly bool _1094Correction;
        private readonly bool _generateXmlOnSchemaError;
        private readonly bool _cleanseInputs;

        /// <param name="pr1094HeaderRow">Contains the PR1094Header reocord for the provided company and tax year.</param>
        /// <param name="pr1095HeaderRows">Contains all child employee/PR1095Header records associated with the employer/PR1094Header.</param>
        /// <param name="pr1095CoveredRows">Contains all child dependent/PR1095Covered records associated with employee/PR1095Header.</param>
        /// <param name="pr1094OtherAleMembers">Contains all subsidiary information associated with the employer/PR1094Header.</param>
        /// <param name="prACAEFileTransmissionHistory">Contains pertinent related data neededed to prevent silly happenings.</param>
        /// <param name="isTest"></param>
        /// <param name="generateXmlOnSchemaError">Non-User modifiable field in PRCO. Allows files to be generated if there is a proble with the schems.</param>
        public FormDataPopulator(DataRow pr1094HeaderRow, DataTable pr1095HeaderRows, DataTable pr1095CoveredRows, DataTable pr1094OtherAleMembers, DataRow prACAEFileTransmissionHistory, bool isTest, bool generateXmlOnSchemaError)
        {
            //Data set objects.
            _pr1094HeaderRow       = pr1094HeaderRow;
            _pr1095HeaderRows      = pr1095HeaderRows;                  
            _pr1095CoveredRows     = pr1095CoveredRows;
            _pr1094OtherMemberRows = pr1094OtherAleMembers;

            //Flags and fields.
            _generateXmlOnSchemaError = generateXmlOnSchemaError;
            _1094Correction           = Helper.GetBool(_pr1094HeaderRow["CorrectionYN"].ToString());
            _correctedReceiptID       = prACAEFileTransmissionHistory["ReceiptID"].ToString();
            _taxYear                  = Convert.ToInt32(pr1094HeaderRow["TaxYear"].ToString());
            _isTest                   = isTest;
            _transmittedEmployeeCount = pr1095HeaderRows.Rows.Count;

            if (_transmittedEmployeeCount > 0)
                _transmittalType = (char)pr1095HeaderRows.Rows[0]["TypeOVC"];

            //If the escape sequence is encountered remove it from the input string and flag to not manipulate output xml.
            var checkForEscapeSequence = pr1094HeaderRow["ALEContactFirstName"].ToString().PadRight(2).Substring(0, 2);
            if (checkForEscapeSequence == ESCAPE_NAME_CONTROL_PROCESSING)
            {
                pr1094HeaderRow["ALEContactFirstName"] = pr1094HeaderRow["ALEContactFirstName"].ToString()
                    .Substring(2, pr1094HeaderRow["ALEContactFirstName"].ToString().Length - 2);
                _cleanseInputs = false;
            }
            else
                _cleanseInputs = true;

        }




        /// <summary>
        /// Loads object tree representing the form data file for the current transmission from the database.  The class used 
        /// to create this object was generated using xsd2code++ in conjuction with the IRS-Form1094-1095CTransmitterUpstreamMessage.xsd
        /// provided by the IRS in the ACA AIR Tax Year schema. 
        /// </summary>
        /// <returns>Object representation of form data file xml.</returns>
        public IRootXmlClass GetXmlRootClassPopulated()
        {
            //In order for the ShouldSerialize Methods methods that control whether or not a complex element gets serialized 
            //(ie, all child elements are empty/null), the calls all have to be made inline such that all child objects are instantiated.
            //If a piece of the giant nested call stack is removed, a null reference exception goes !boom!.
            return new Form109495CTransmittalUpstreamType {
                Form1094CUpstreamDetail = new List<Form1094CUpstreamDetailType> {
                    _PopulateForm1094CUpstreamDetail(_pr1094HeaderRow, _pr1094OtherMemberRows, _taxYear, _transmittedEmployeeCount),
                },
            };
        }


        /// <summary>
        /// Method to populate the Form1094CUpstreamDetailType.  This is defined as a collection in the parent element, however 
        /// the collection will only have one element as we only need to accomodate submitting a single company/1094C.   Non-required 
        /// elements are set to null. This data corresponds to information about the employer in the PR1094Header, PR1094ALEMemberInfo 
        /// and PR1094ALEOtherMembers tables entered in the PR ACA Process form in the corresponding tabs:  Info (Employer Information), 
        /// ALE Member Info - Monthly (Full and Part time employee counts by month), and ALE Group Members (subsidiary company names 
        /// and EINs).  Non-required elements are nulled out, or not initialized to suppress the element in the generated xml.
        /// </summary>
        /// <param name="pr1094HeaderRow">Employer information.</param>
        /// <param name="pr1094OtherAleMembers">Employer subsidiary information.</param>
        /// <param name="taxYear">Tax year for which the transmission is being submitted.</param>
        /// <param name="transmittedEmployeeCount">Number of employees contained in transmission.</param>
        /// <returns></returns>
        private Form1094CUpstreamDetailType _PopulateForm1094CUpstreamDetail(DataRow pr1094HeaderRow, DataTable pr1094OtherAleMembers, int taxYear, int transmittedEmployeeCount)
        {
             return new Form1094CUpstreamDetailType {
                //Not Used
                GovtEntityEmployerInfoGrp  = null,
                JuratSignaturePIN          = null,
                PersonTitleTxt             = null,
                TestScenarioId             = null,
                OriginalUniqueSubmissionId = null,

                //Constants
                lineNum      = LINE_NUMBER_IRS_CONSTANT,
                recordType   = RECORD_TYPE_IRS_CONSTANT,
                SubmissionId = SUBMISSION_ID,

                //Employees Data. 
                Form1095CUpstreamDetail = _PopulateForm1095CUpstreamDetail(_pr1094HeaderRow, _pr1095HeaderRows, _pr1095CoveredRows, _taxYear, _transmittalType, _correctedReceiptID),
                EmployerInformationGrp  = _PopulateEmployerInformationGrpType(pr1094HeaderRow),
                OtherALEMembersGrp      = _transmittalType == 'C' ? null : _populateOtherAleMembers(pr1094OtherAleMembers),
                ALEMemberInformationGrp = _transmittalType == 'C' ? null : _PopulateALEMemberInformationGroup(pr1094HeaderRow),

                //Employer Flags -- ommitted on employee/1095 corrections due to issues with AATS.
                AggregatedGroupMemberCd       = _transmittalType == 'C' ? DigitCodeType.False    : Helper.GetDigitCode(pr1094HeaderRow["IsMemberOfALEGroup"].ToString(), SerializeElement: true),
                AuthoritativeTransmittalInd   = _transmittalType == 'C' ? DigitBooleanType.False : Helper.GetBoolDigit(pr1094HeaderRow["AuthoritativeTransmittal"].ToString()),
                NinetyEightPctOfferMethodInd  = _transmittalType == 'C' ? DigitBooleanType.False : Helper.GetBoolDigit(pr1094HeaderRow["NinetyEightPctOffer"].ToString()),
                Section4980HReliefInd         = _transmittalType == 'C' ? DigitBooleanType.False : Helper.GetBoolDigit(pr1094HeaderRow["Sec4980HTransRelief"].ToString()),
                QualifyingOfferMethodInd      = _transmittalType == 'C' ? DigitBooleanType.False : Helper.GetBoolDigit(pr1094HeaderRow["QualOfferMethod"].ToString()),

                //Transmission metadata
                Form1095CAttachedCnt       = transmittedEmployeeCount.ToString(),
                TotalForm1095CALEMemberCnt = _transmittalType == 'C' ? null : transmittedEmployeeCount.ToString(),
                SignatureDt                =  DateTime.Now.ToString("yyyy-MM-dd"),
                TaxYr                      = taxYear.ToString(),

                //Correction information
                CorrectedInd               = _1094Correction ? DigitBooleanType.True : DigitBooleanType.False,
                CorrectedSubmissionInfoGrp = _1094Correction ? _populateCorrectedSubmissionInfoGrp(pr1094HeaderRow) : null
            };
        }

        /// <summary>
        ///     Removes all non alpha-numeric characters except '&' and '-' then replaces the only possibly remaining restricted
        ///     character.  In case fo failure if "***" is encountered the string manipulation is bypassed.
        ///     see https://www.irs.gov/Businesses/Corporations/Using-the-Correct-Name-Control-in-e-filing-Corporate-Tax-Returns  
        /// </summary>
        private string getNameControl(string stringToParse)
        {
            try
            {
                return _cleanseInputs
                    ? Regex.Replace(stringToParse ?? string.Empty, @"[^A-Za-z0-9&-]", string.Empty).PadRight(4).Substring(0, 4).ToUpper().Trim()
                    : stringToParse.Trim().PadRight(4).Substring(4).ToUpper().Trim();
            }
            catch
            {
                return stringToParse;
            } 
        }

        /// <summary>
        ///     Removes all illegal characters according to schema regex.
        /// </summary>
        private string cleanseBusinessName(string stringToParse)
        {
            try
            {
                return _cleanseInputs
                    ? Regex.Replace(stringToParse ?? string.Empty, @"[^A-Za-z0-9-& ]", string.Empty).Trim()
                    : stringToParse.Trim().ToUpper();
            }
            catch
            {
                return stringToParse?.Trim().ToUpper() ?? string.Empty;
            }
        }

        /// <summary>
        ///     Removes all special characters according..
        /// </summary>
        private string cleanseInput(string stringToParse)
        {
            try
            { 
                return _cleanseInputs 
                    ? Regex.Replace(stringToParse ?? string.Empty, @"[^A-Za-z0-9- ]", string.Empty).Trim()
                    : stringToParse.Trim();
            }
            catch
            {
                return stringToParse?.Trim() ?? string.Empty;
            } 
        }
                                                        
        private EmployerInformationGrpType _PopulateEmployerInformationGrpType(DataRow pr1094HeaderRow)
        {         
            return new EmployerInformationGrpType {
                BusinessName = {
                    BusinessNameLine1Txt = cleanseBusinessName(pr1094HeaderRow["ALEName"].ToString().Trim()),
                    BusinessNameLine2Txt = null
                },                                                      
                ContactNameGrp = {
                    PersonFirstNm  = cleanseInput(pr1094HeaderRow["ALEContactFirstName"].ToString()),
                    PersonMiddleNm = null,
                    PersonLastNm   = cleanseInput(pr1094HeaderRow["ALEContactLastName"].ToString()),
                    SuffixNm       = null
                },
                MailingAddressGrp = {
                    Item = new USAddressGrpType {
                        AddressLine1Txt  = cleanseInput(pr1094HeaderRow["ALEAddress"].ToString()),
                        AddressLine2Txt  = null,
                        CityNm           = cleanseInput(Helper.GetNullOrValue(pr1094HeaderRow["ALECity"])),
                        USStateCd        = Helper.GetEmployerStateType(pr1094HeaderRow["ALEState"], _isTest, _generateXmlOnSchemaError),
                        USZIPCd          = cleanseInput(pr1094HeaderRow["USZipCd"].ToString().Trim()),
                        USZIPExtensionCd = null
                    }
                },
                BusinessNameControlTxt = getNameControl(pr1094HeaderRow["ALEName"].ToString()), 
                ContactPhoneNum        = cleanseInput(Helper.GetNullOrValue(pr1094HeaderRow["formattedPhoneNum"])),
                EmployerEIN            = cleanseInput(Helper.GetNullOrValue(pr1094HeaderRow["formattedEIN"])),
                TINRequestTypeCd       = TINRequestTypeCodeType.BUSINESS_TIN
            };
        }

        #region Populate ALEMemberInformationGroupType
        private ALEMemberInformationGrpType _PopulateALEMemberInformationGroup(DataRow pr1094HeaderRow)
        {
            var aleMemberInformationGrp = new ALEMemberInformationGrpType();

            aleMemberInformationGrp.YearlyALEMemberDetail = PopulateALEAnnualInfoGroupType(pr1094HeaderRow);

            aleMemberInformationGrp.JanALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Jan", pr1094HeaderRow);
            aleMemberInformationGrp.FebALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Feb", pr1094HeaderRow);
            aleMemberInformationGrp.MarALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Mar", pr1094HeaderRow);
            aleMemberInformationGrp.AprALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Apr", pr1094HeaderRow);
            aleMemberInformationGrp.MayALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("May", pr1094HeaderRow);
            aleMemberInformationGrp.JunALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Jun", pr1094HeaderRow);
            aleMemberInformationGrp.JulALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Jul", pr1094HeaderRow);
            aleMemberInformationGrp.AugALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Aug", pr1094HeaderRow);
            aleMemberInformationGrp.SeptALEMonthlyInfoGrp = PopulateALEMonthlyInfoGrp("Sep", pr1094HeaderRow);
            aleMemberInformationGrp.OctALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Oct", pr1094HeaderRow);
            aleMemberInformationGrp.NovALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Nov", pr1094HeaderRow);
            aleMemberInformationGrp.DecALEMonthlyInfoGrp  = PopulateALEMonthlyInfoGrp("Dec", pr1094HeaderRow);

            return GetIRSCompliantFlags(aleMemberInformationGrp);
        }

        private ALEMemberInformationGrpType GetIRSCompliantFlags(ALEMemberInformationGrpType aleMemberInformationGrp)
        {
            // These two flags implemented a dataa type we didn't utilize.  In order to not touch the generated code,
            // this was workedaround by setting the header to not serialize when false, and proccesing with this method.
            // Problem was no way to differentiate between no and not checked. 


            //Top down approach.If the Yearly flag is set to true the user must have touched it.  
            //hide the monthly detail flags accordingly to pass pass testing.
            if (aleMemberInformationGrp.YearlyALEMemberDetail.AggregatedGroupInd == DigitBooleanType.True) {
                HideMonthlyAggFlags(aleMemberInformationGrp);
            }
            //When the yearly flag is false and all monthlies true, output in same state as above for consistency.
            //If the yearly is False / No leaving in there 0 - 11 true months.
            else if (aleMemberInformationGrp.AllMonthlyAggregateGrpFlagsAreTrue()) {
                aleMemberInformationGrp.YearlyALEMemberDetail.AggregatedGroupInd = DigitBooleanType.True;
                HideMonthlyAggFlags(aleMemberInformationGrp);
            }


            if (aleMemberInformationGrp.YearlyALEMemberDetail.MinEssentialCvrOffrCd == DigitCodeType.True) {
                HideMonthlyMECFlags(aleMemberInformationGrp);
            }
            else if (aleMemberInformationGrp.AllMonthlyMECFlagsAreTrue()) {
                aleMemberInformationGrp.YearlyALEMemberDetail.MinEssentialCvrOffrCd = DigitCodeType.False;
                HideMonthlyMECFlags(aleMemberInformationGrp);
            }

            return aleMemberInformationGrp;
        }

        private ALEMemberAnnualInfoGrpType PopulateALEAnnualInfoGroupType(DataRow pr1094HeaderRow)
        {
            var fullTimeCount = pr1094HeaderRow["Yearly_FullTimeEmplCount"].ToString();
            var totalEmpCount = pr1094HeaderRow["Yearly_TotalEmplCount"].ToString();

            return new ALEMemberAnnualInfoGrpType() {
                AggregatedGroupInd        = Helper.GetBoolDigit(pr1094HeaderRow["Yearly_AggregatedGroupFlag"].ToString()),
                MinEssentialCvrOffrCd     = Helper.GetDigitCode(pr1094HeaderRow["Yearly_MECFlag"].ToString(), SerializeElement: false),
                ALEMemberFTECnt           = Helper.IsZeroOrWhitespace(fullTimeCount) ? null : fullTimeCount,
                TotalEmployeeCnt          = Helper.IsZeroOrWhitespace(totalEmpCount) ? null : totalEmpCount,
                ALESect4980HTrnstReliefCd = Helper.GetNullOrValue(pr1094HeaderRow["Yearly_TransReliefFlag"].ToString())
            };
        }

        private ALEMemberMonthlyInfoGrpType PopulateALEMonthlyInfoGrp(string month, DataRow pr1094HeaderRow)
        {
            var fullTimeCount = pr1094HeaderRow[month + "_FullTimeEmplCount"].ToString();
            var totalEmpCount = pr1094HeaderRow[month + "_TotalEmplCount"].ToString();

            return new ALEMemberMonthlyInfoGrpType {
                AggregatedGroupInd        = Helper.GetBoolDigit(pr1094HeaderRow[month + "_AggregatedGroupFlag"].ToString()),
                MinEssentialCvrOffrCd     = Helper.GetDigitCode(pr1094HeaderRow[month + "_MECFlag"].ToString(), SerializeElement: true),
                ALEMemberFTECnt           = Helper.IsZeroOrWhitespace(fullTimeCount) ? null : fullTimeCount,
                TotalEmployeeCnt          = Helper.IsZeroOrWhitespace(totalEmpCount) ? null : totalEmpCount,
                ALESect4980HTrnstReliefCd = Helper.GetNullOrValue(pr1094HeaderRow[month + "_TransReliefFlag"].ToString()) 
            };
        }

        private void HideMonthlyAggFlags(ALEMemberInformationGrpType aleMemberInformationGrp)
        {
            aleMemberInformationGrp.JanALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.FebALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.MarALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.AprALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.MayALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.JunALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.JulALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.AugALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.SeptALEMonthlyInfoGrp.AggregatedGroupInd = DigitBooleanType.False;
            aleMemberInformationGrp.OctALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.NovALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
            aleMemberInformationGrp.DecALEMonthlyInfoGrp.AggregatedGroupInd  = DigitBooleanType.False;
        }

        private void HideMonthlyMECFlags(ALEMemberInformationGrpType aleMemberInformationGrp)
        {
            aleMemberInformationGrp.JanALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.FebALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.MarALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.AprALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.MayALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.JunALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.JulALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.AugALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.SeptALEMonthlyInfoGrp.MinEssentialCvrOffrCd = DigitCodeType.False;
            aleMemberInformationGrp.OctALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.NovALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
            aleMemberInformationGrp.DecALEMonthlyInfoGrp.MinEssentialCvrOffrCd  = DigitCodeType.False;
        }

        private List<OtherALEMembersType> _populateOtherAleMembers(DataTable pr1094OtherAleMembers)
        {
            var otherAleMembers = new List<OtherALEMembersType>();

            for (int currRowIndex = 0; currRowIndex < pr1094OtherAleMembers.Rows.Count; currRowIndex++)
            {
                var otherAleMember = new OtherALEMembersType {
                    BusinessName = new BusinessNameType {
                        BusinessNameLine1Txt = cleanseBusinessName(pr1094OtherAleMembers.Rows[currRowIndex]["MemberName"].ToString().Trim()),
                        BusinessNameLine2Txt = null
                    },
                    BusinessNameControlTxt = getNameControl(pr1094OtherAleMembers.Rows[currRowIndex]["MemberName"].ToString()),
                    TINRequestTypeCd       = TINRequestTypeCodeType.BUSINESS_TIN,
                    EIN                    = cleanseInput(pr1094OtherAleMembers.Rows[currRowIndex]["formattedEIN"].ToString())
                };
                otherAleMembers.Add(otherAleMember);
            }
            return otherAleMembers;
        }
        #endregion

        private CorrectedSubmissionInfoGrpType _populateCorrectedSubmissionInfoGrp(DataRow pr1094HeaderRow)
        {
            return new CorrectedSubmissionInfoGrpType {
                //Let corrections be sent with business name as submitted.
                CorrectedSubmissionPayerName = new BusinessNameType{
                    BusinessNameLine1Txt = cleanseBusinessName(pr1094HeaderRow["ALEName"].ToString()),
                    BusinessNameLine2Txt = null 
                },
                CorrectedSubmissionPayerTIN = cleanseInput(pr1094HeaderRow["formattedEIN"].ToString()),
                CorrectedUniqueSubmissionId = _correctedReceiptID + "|1"
            };
        }

        #region Methods to populate the Form1095CUpstreamDetailType List
        /// <summary>
        /// Method to populate the Form1095CUpstreamDetailType list.  Non-required elements are set to null.  This data corresponds to 
        /// informationabout each employee in the PR1095Header table entereed in the PR ACA Process form in the corresponding Employee tab.
        /// Additional data from PR1095EmployeeDetail and PR1095Covered entered in the PR ACA Process -> PR ACA 1095-C Employee subform in 
        /// the corresponding tabs:  Monthly offer of Coverage (Coverage Offer and Acceptance History) and Covered Indidividuals (Dependent 
        /// Coverage History).   Non-required elements are nulled out or not initialized.
        /// </summary>
        /// <param name="pr1094HeaderRow">Used only to populate the ALEContactPhoneNumber element.</param>
        /// <param name="pr1095HeaderRows">Employee coverage history.</param>
        /// <param name="pr1095CoveredRows">Dependent coverage history needed for employers with self insured plans.</param>
        /// <param name="taxYear">Tax year for which the transmission is being submitted.</param>
        /// <param name="transmittalType">'O'riginal, 'R'eplacement, 'C'orrection.  See IRS publications 5164 and 5165.</param>
        /// <param name="correctedReceiptID">The receiptID of the transmission being corrected.</param>
        /// <returns></returns>
        private List<Form1095CUpstreamDetailType> _PopulateForm1095CUpstreamDetail(DataRow pr1094HeaderRow, DataTable pr1095HeaderRows, DataTable pr1095CoveredRows, int taxYear, char? transmittalType, string correctedReceiptID)
        {
            var form1095CUpstreamDetails = new List<Form1095CUpstreamDetailType>();

            for (var current1095HeaderRow = 0; current1095HeaderRow < pr1095HeaderRows.Rows.Count; current1095HeaderRow++)
            {
                bool IsSelfInsured = Helper.IsTrueDigitBool(Helper.GetBoolDigit(pr1095HeaderRows.Rows[current1095HeaderRow]["SelfInsCoverage"].ToString()));
                bool IsCorrection = transmittalType == 'C';

                var form1095CUpstreamDetail = new Form1095CUpstreamDetailType {
                    //Employee header metadata.
                    RecordId           = pr1095HeaderRows.Rows[current1095HeaderRow]["NewRecordID"].ToString(),
                    ALEContactPhoneNum = Helper.GetNullOrValue(pr1094HeaderRow["formattedPhoneNum"]),
                    TaxYr              = taxYear.ToString(),
                    recordType         = RECORD_TYPE_IRS_CONSTANT,
                    lineNum            = LINE_NUMBER_IRS_CONSTANT,
                    TestScenarioId     = null,
                    StartMonthNumberCd = pr1095HeaderRows.Rows[current1095HeaderRow]["PlanStartMonth"].ToString(),   

                    //Populate correction flag and object.
                    CorrectedInd           = IsCorrection ? DigitBooleanType.True : DigitBooleanType.False,
                    CorrectedRecordInfoGrp = IsCorrection ? _PopulateCorrectedRecordInfoGrp(pr1095HeaderRows.Rows[current1095HeaderRow], correctedReceiptID) : null,

                    //Populate employee 1095C data.
                    EmployeeOfferAndCoverageGrp = _PopulateEmployeeOfferAndCoverageGroup(pr1095HeaderRows.Rows[current1095HeaderRow]) ?? new EmployeeOfferAndCoverageGrpType(),
                    EmployeeInfoGrp             = _PopulateEmployeeInformationGrpType(pr1095HeaderRows.Rows[current1095HeaderRow])    ?? new EmployeeInformationGrpType(),

                    //Populate 1095C-III dependent data for employee.
                    CoveredIndividualInd = IsSelfInsured ? DigitBooleanType.True : DigitBooleanType.False,
                    CoveredIndividualGrp = _PopulateCoveredIndividualGroup(pr1095CoveredRows, Convert.ToInt32(pr1095HeaderRows.Rows[current1095HeaderRow]["Employee"])),
                };

                form1095CUpstreamDetails.Add(form1095CUpstreamDetail);
            }

            return form1095CUpstreamDetails;
        }


        private CorrectedRecordInfoGrpType _PopulateCorrectedRecordInfoGrp(DataRow currentPR1095HeaderRow, string correctedReceiptID)
        {
            return new CorrectedRecordInfoGrpType {
                CorrectedRecordPayeeName = new OtherCompletePersonNameType {
                    //Let Names be submitted as is for corrections.
                    PersonFirstNm = currentPR1095HeaderRow["FirstName"].ToString(),
                    PersonLastNm  = currentPR1095HeaderRow["LastName"].ToString()
                },
                CorrectedRecordPayeeTIN = cleanseInput(Helper.GetNullOrValue(currentPR1095HeaderRow["formattedSSN"])),
                CorrectedUniqueRecordId = string.Format("{0}|{1}|{2}", correctedReceiptID, SUBMISSION_ID, currentPR1095HeaderRow["RecordID"]),
            };
        }


                                                                
        private EmployeeInformationGrpType _PopulateEmployeeInformationGrpType(DataRow currentPR1095HeaderRow)
        {
            return new EmployeeInformationGrpType {
                OtherCompletePersonName = {
                    PersonFirstNm        = cleanseInput(currentPR1095HeaderRow["FirstName"].ToString()),
                    PersonMiddleNm       = null,
                    PersonLastNm         = cleanseInput(currentPR1095HeaderRow["LastName"].ToString()),
                    SuffixNm             = null
                },   
                MailingAddressGrp = {
                    Item = new USAddressGrpType {
                        AddressLine1Txt  = cleanseInput(currentPR1095HeaderRow["Address"].ToString()),
                        AddressLine2Txt  = null,
                        CityNm           = cleanseInput(currentPR1095HeaderRow["City"].ToString()),
                        USStateCd        = Helper.GetEmployeeStateType(currentPR1095HeaderRow["State"], _isTest, _generateXmlOnSchemaError),
                        USZIPCd          = cleanseInput(currentPR1095HeaderRow["USZIPCd"].ToString()),
                        USZIPExtensionCd = null
                    }
                },
                TINRequestTypeCd     = TINRequestTypeCodeType.INDIVIDUAL_TIN,
                SSN                  = cleanseInput(currentPR1095HeaderRow["formattedSSN"].ToString()),
                PersonNameControlTxt = getNameControl(currentPR1095HeaderRow["LastName"].ToString()),
            };
        }



        private EmployeeOfferAndCoverageGrpType _PopulateEmployeeOfferAndCoverageGroup(DataRow currentPR1095HeaderRow)
        {
            var employeeOfferAndCoverageGrp = new EmployeeOfferAndCoverageGrpType();

            //If the annual safe harbor flag is the default value load the monthly flags.
            employeeOfferAndCoverageGrp.AnnualOfferOfCoverageCd = Helper.GetNullOrValue(currentPR1095HeaderRow["OfferCoverageAll12Mths"]);
            if (string.IsNullOrWhiteSpace(employeeOfferAndCoverageGrp.AnnualOfferOfCoverageCd))
            {
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.JanOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Jan_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.FebOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Feb_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.MarOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Mar_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.AprOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Apr_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.MayOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["May_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.JunOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Jun_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.JulOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Jul_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.AugOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Aug_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.SepOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Sep_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.OctOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Oct_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.NovOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Nov_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.DecOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Dec_CoverageOffer"].ToString());
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp.DecOfferCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Dec_CoverageOffer"].ToString());
            }
            //The annual flag is populated so kill the detail records.  
            else
            {
                employeeOfferAndCoverageGrp.MonthlyOfferCoverageGrp = null;
            }


            //If the annual safe harbor flag is the default value load the monthly flags.
            employeeOfferAndCoverageGrp.AnnualSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Section4980HAll12Mths"]);
            if (string.IsNullOrWhiteSpace(employeeOfferAndCoverageGrp.AnnualOfferOfCoverageCd))
            {
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.JanSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Jan_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.FebSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Feb_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.MarSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Mar_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.AprSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Apr_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.MaySafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["May_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.JunSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Jun_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.JulSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Jul_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.AugSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Aug_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.SepSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Sep_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.OctSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Oct_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.NovSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Nov_SafeHarbor"].ToString());
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp.DecSafeHarborCd = Helper.GetNullOrValue(currentPR1095HeaderRow["Dec_SafeHarbor"].ToString());
            }
            //The annual flag is populated so kill the detail records.  
            else
            {
                employeeOfferAndCoverageGrp.MonthlySafeHarborGrp = null;
            }

            //If the annual amount is the default value load the monthly flags.
            employeeOfferAndCoverageGrp.AnnlShrLowestCostMthlyPremAmt = Helper.GetNullOrValue(currentPR1095HeaderRow["EmployeeShareAll12Mths"].ToString());
            if (string.IsNullOrWhiteSpace(employeeOfferAndCoverageGrp.AnnlShrLowestCostMthlyPremAmt))
            {
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.JanuaryAmt   = Helper.GetNullOrValue(currentPR1095HeaderRow["Jan_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.FebruaryAmt  = Helper.GetNullOrValue(currentPR1095HeaderRow["Feb_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.MarchAmt     = Helper.GetNullOrValue(currentPR1095HeaderRow["Mar_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.AprilAmt     = Helper.GetNullOrValue(currentPR1095HeaderRow["Apr_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.MayAmt       = Helper.GetNullOrValue(currentPR1095HeaderRow["May_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.JuneAmt      = Helper.GetNullOrValue(currentPR1095HeaderRow["Jun_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.JulyAmt      = Helper.GetNullOrValue(currentPR1095HeaderRow["Jul_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.AugustAmt    = Helper.GetNullOrValue(currentPR1095HeaderRow["Aug_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.SeptemberAmt = Helper.GetNullOrValue(currentPR1095HeaderRow["Sep_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.OctoberAmt   = Helper.GetNullOrValue(currentPR1095HeaderRow["Oct_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.NovemberAmt  = Helper.GetNullOrValue(currentPR1095HeaderRow["Nov_ShareAmount"].ToString());
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp.DecemberAmt  = Helper.GetNullOrValue(currentPR1095HeaderRow["Dec_ShareAmount"].ToString());
            }
            /* Deault of Blank was changed to non-blank.  Suppress XML by leaving months null. */
            else
            {
                employeeOfferAndCoverageGrp.MonthlyShareOfLowestCostMonthlyPremGrp = null;
            }

            return employeeOfferAndCoverageGrp;
        }


        private List<EmployerCoveredIndividualType> _PopulateCoveredIndividualGroup(DataTable pr1095CoveredRows, int employee)
        {
            var employerCoveredIndividuals = new List<EmployerCoveredIndividualType>();
            var employeeDependents         = pr1095CoveredRows.Select(string.Format("Employee = {0}", employee));

            for (var currentDependentIndex = 0; currentDependentIndex < employeeDependents.Length; currentDependentIndex++)
            {
                bool annualCovereageOffered = Helper.IsTrue(employeeDependents[currentDependentIndex]["CoveredAll12Months"].ToString());
                bool SSNExists = !string.IsNullOrEmpty(employeeDependents[currentDependentIndex]["formattedSSN"].ToString());

                var employerCoveredIndividual = new EmployerCoveredIndividualType {
                    //Form 1094-C III data.
                    SSN                   =  SSNExists ? employeeDependents[currentDependentIndex]["formattedSSN"].ToString() : null,
                    BirthDt               = !SSNExists ? employeeDependents[currentDependentIndex]["formattedDOB"].ToString() : null,                   
                    TINRequestTypeCd      = TINRequestTypeCodeType.INDIVIDUAL_TIN,
                    PersonNameControlTxt  = getNameControl(employeeDependents[currentDependentIndex]["LastName"].ToString()),   
                    
                    CoveredIndividualName = {
                        PersonFirstNm  = cleanseInput(employeeDependents[currentDependentIndex]["FirstName"].ToString()),
                        PersonMiddleNm = null,
                        PersonLastNm   = cleanseInput(employeeDependents[currentDependentIndex]["LastName"].ToString()),
                        SuffixNm       = null
                    },

                    //Populate dependent offer of coverage data.
                    CoveredIndividualAnnualInd     = annualCovereageOffered ? DigitBooleanType.True : DigitBooleanType.False,
                    CoveredIndividualMonthlyIndGrp = _PopulateMonthIndGrpType(employeeDependents[currentDependentIndex]), 
                };
                employerCoveredIndividuals.Add(employerCoveredIndividual);
            }
            return employerCoveredIndividuals;
        }


        /// <summary>
        /// Populates MonthIndGrptype from PR1095CoveredRow.
        /// </summary>
        /// <param name="dependent"></param>
        /// <returns cref="MonthIndGrpType"></returns>
        private MonthIndGrpType _PopulateMonthIndGrpType(DataRow dependent)
        {
            return new MonthIndGrpType {
                JanuaryInd   = Helper.GetBoolDigit(dependent["Jan"].ToString()),
                FebruaryInd  = Helper.GetBoolDigit(dependent["Feb"].ToString()),
                MarchInd     = Helper.GetBoolDigit(dependent["Mar"].ToString()),
                AprilInd     = Helper.GetBoolDigit(dependent["Apr"].ToString()),
                MayInd       = Helper.GetBoolDigit(dependent["May"].ToString()),
                JuneInd      = Helper.GetBoolDigit(dependent["June"].ToString()),
                JulyInd      = Helper.GetBoolDigit(dependent["July"].ToString()),
                AugustInd    = Helper.GetBoolDigit(dependent["Aug"].ToString()),
                SeptemberInd = Helper.GetBoolDigit(dependent["Sept"].ToString()),
                OctoberInd   = Helper.GetBoolDigit(dependent["Oct"].ToString()),
                NovemberInd  = Helper.GetBoolDigit(dependent["Nov"].ToString()),
                DecemberInd  = Helper.GetBoolDigit(dependent["Dec"].ToString())
            };
        }
        #endregion
    }
}