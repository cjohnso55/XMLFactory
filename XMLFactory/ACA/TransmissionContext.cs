using System;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO;
using XmlFactory.Common;
using XMLFactory.Common;

namespace XMLFactory.ACA
{
    /// <summary>
    /// Immutable wrapper that encapsulate the form state associated with the current transmission along with the associated data. 
    /// </summary>
    public class TransmissionContext
    {
        public const bool  PriorYearDataInd    = false;
        public const string DateTimeFormat = "yyyyMMddTHHmmssfffZ";

        public readonly Guid    Uuid = Guid.NewGuid();
        public readonly string  FilePathTimeStamp = DateTime.Now.ToString(DateTimeFormat);

        public readonly int TaxYear;
        public readonly int PaymentYear;
        public readonly int Company;
        public readonly int TransmissionID;

        public readonly string  DefaultSaveDirectoryPath;
        public readonly string  TransmissionType;
        public readonly string  FormDataFilePath;
        public readonly string  ManifestFilePath;
        public readonly string  TransmitterControlCode;
        public readonly string  CorrectedReceiptID;

        public readonly bool IsTest;
        public readonly bool GenerateXmlOnSchemaError;


        public readonly DataSet   TransmissionDataSet;

        public readonly DataTable EmployeesToTransmit;
        public readonly DataTable DependentsToTransmit;
        public readonly DataTable EmployerSubsidiariesToTransmit;
        public readonly DataTable TransmissionHistory;

        public readonly DataRow CorrectedTransmissionInfo;
        public readonly DataRow EmployerInfoToTransmit;

        public TransmissionContext() { }

        public TransmissionContext(int taxYear, int paymentYear, int company, bool isTest, string defaultSaveDirectoryPath, string transmissionType)
        {
            TaxYear = taxYear;
            Company = company;
            IsTest  = isTest;
            TransmissionType = IsTest ? "O" : transmissionType;
            PaymentYear = paymentYear;
            DefaultSaveDirectoryPath = defaultSaveDirectoryPath;

            TransmitterControlCode   = _getTransmitterControlCodeFromDB();
            GenerateXmlOnSchemaError = _getGenerateXmlOnSchemaErrorFromDB();
            CorrectedReceiptID       = _getCorrectedReceiptIDFromDB();
            TransmissionDataSet      = _getTransmissionDataSetFromDB();

            EmployeesToTransmit            = TransmissionDataSet.Tables[1];
            DependentsToTransmit           = TransmissionDataSet.Tables[2];
            EmployerSubsidiariesToTransmit = TransmissionDataSet.Tables[3];
            EmployerInfoToTransmit         = TransmissionDataSet.Tables[0].Rows.Count > 0 ? TransmissionDataSet.Tables[0].Rows[0] : TransmissionDataSet.Tables[0].NewRow();
            CorrectedTransmissionInfo      = TransmissionDataSet.Tables[4].Rows.Count > 0 ? TransmissionDataSet.Tables[4].Rows[0] : TransmissionDataSet.Tables[4].NewRow();
            TransmissionID                 = EmployeesToTransmit.Rows.Count > 0 ? Convert.ToInt32(EmployeesToTransmit.Rows[0]["NewTransmissionID"]) : 0;
            TransmissionHistory            = TransmissionDataSet.Tables[5];        

            FormDataFilePath = _createFormDataFilePath();
            ManifestFilePath = _createManifestFilePath();
        }

        /// <summary>
        /// Retrieves the TransmitterControlCode from dbo.PRCO.
        /// </summary>
        private string _getTransmitterControlCodeFromDB()
        {
            var sqlCommand = new SqlCommand("Select   Top(1) TCC   " +
                                                "from  dbo.PRCO     " +
                                                "where PRCo = @PRCo " +
                                                "order by GLCo      ");

            sqlCommand.Parameters.Add(new SqlParameter() { ParameterName = "@PRCo", SqlDbType = SqlDbType.TinyInt, Value = Company });
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.Connection = Helper.get_UserConnection(null);

            return SqlHelper.ExecuteScalar(sqlCommand).ToString();
        }

        /// <summary>
        /// Creates schema compliant file path of the form data file.
        /// </summary>
        private string _createFormDataFilePath()
        {
            var filename = string.Format(@"{0}_{1}_{2}.xml",
                "1094C_Request",
                TransmitterControlCode,
                FilePathTimeStamp);

            return Path.Combine(DefaultSaveDirectoryPath, filename); 
        }

        /// <summary>
        /// The schema compliant filepath of the manifest file.
        /// </summary>
        private string _createManifestFilePath()
        {
                var filename = string.Format(@"{0}_{1}_{2}.xml",
                    "1094C_Manifest",
                    TransmitterControlCode,
                    FilePathTimeStamp);

                return Path.Combine(DefaultSaveDirectoryPath, filename);
        }

        /// <summary>
        /// Retrieves the GenerateXmlOnSchemaError flag from dbo.PRCO.
        /// </summary>
        private bool _getGenerateXmlOnSchemaErrorFromDB()
        {
                var sqlCommand = new SqlCommand("Select   Top(1) GenerateACAXmlOnSchemaErrorYN " +
                                                   "from  dbo.PRCO     " +
                                                   "where PRCo = @PRCo " +
                                                   "order by GLCo      ");

                sqlCommand.Parameters.Add(new SqlParameter() { ParameterName = "@PRCo", SqlDbType = SqlDbType.TinyInt, Value = Company });
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Connection = Helper.get_UserConnection(null);

                return SqlHelper.ExecuteScalar(sqlCommand).ToString() == "Y";
        }


        /// <summary>
        /// Retrieves the receiptID of the previous transmission for a company/tax year.  
        /// </summary>
        private string _getCorrectedReceiptIDFromDB()
        {          
            var getPreviousTransIDCommand = new SqlCommand("Select IsNull(max(hist.TransmissionID), 0)       " +
                                                                "from  dbo.ACAeFileTransmissionHistory hist  " + 
                                                                "where hist.TaxYear = @TaxYear " +
                                                                "and   hist.PRCo    = @PRCo    ");

            getPreviousTransIDCommand.Parameters.Add(new SqlParameter() { ParameterName = "@PRCo",    SqlDbType = SqlDbType.TinyInt, Value = Company });
            getPreviousTransIDCommand.Parameters.Add(new SqlParameter() { ParameterName = "@TaxYear", SqlDbType = SqlDbType.Int, Value = TaxYear });

            getPreviousTransIDCommand.CommandType = CommandType.Text;
            getPreviousTransIDCommand.Connection = Helper.get_UserConnection(null);
                
            int previousTransmissionID = (int)SqlHelper.ExecuteScalar(getPreviousTransIDCommand);

            //No previous transmission exists.  No reason to hit the DB again.
            if (previousTransmissionID == 0)
                return string.Empty;

            //The IRS spec specifies replacements should always reference the receiptID of the original transmission.
            if (TransmissionType == "R")
                previousTransmissionID = 1;

            var getCorrectedRecieptIDCommand = new SqlCommand("Select IsNull(hist.ReceiptID, '')  " +
                                                                    "from dbo.PRACAeFileTransmissionHistory hist " + 
                                                                    "where hist.TransmissionID = @PreviousTransmissionID " +
                                                                    " and hist.TaxYear = @TaxYear  " +
                                                                    " and hist.PRCo = @PRCo");

            getCorrectedRecieptIDCommand.Parameters.Add(new SqlParameter() { ParameterName = "@PRCo",    SqlDbType = SqlDbType.TinyInt, Value = Company });
            getCorrectedRecieptIDCommand.Parameters.Add(new SqlParameter() { ParameterName = "@TaxYear", SqlDbType = SqlDbType.Int,     Value = TaxYear });
            getCorrectedRecieptIDCommand.Parameters.Add(new SqlParameter() { ParameterName = "@PreviousTransmissionID",  SqlDbType = SqlDbType.Int, Value = previousTransmissionID });

            getCorrectedRecieptIDCommand.CommandType = CommandType.Text;
            getCorrectedRecieptIDCommand.Connection = Helper.get_UserConnection(null);

            return SqlHelper.ExecuteScalar(getCorrectedRecieptIDCommand).ToString();
        }

        /// <summary>
        /// Returns all the data points needed to populate the form data and manifest xml.
        /// </summary>
        private DataSet _getTransmissionDataSetFromDB()
        {
            SqlCommand cmd;

            if (IsTest)
            {
                cmd = new SqlCommand("vspPRACAEFileSubmissionTest", Helper.get_UserConnection()) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("@PRCo", Company);
                cmd.Parameters.AddWithValue("@TaxYear", TaxYear);
            }
            else
            {
                cmd = new SqlCommand("vspPRACAEFileGetTransmissionDataSet", Helper.get_UserConnection()) { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.AddWithValue("@PRCo", Company);
                cmd.Parameters.AddWithValue("@TaxYear", TaxYear);
                cmd.Parameters.AddWithValue("@TransmissionType", TransmissionType);
                cmd.Parameters.Add("@ErrorMessage", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;
            }
            
            using (var da = new SqlDataAdapter(cmd))
            {
                var transmissionDataTables = new DataSet();
                da.Fill(transmissionDataTables);
                return transmissionDataTables;
            }
        }
    }
}
