using System;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using XMLFactory.ACA.FormData;
using XMLFactory.Interfaces;

namespace XMLFactory.Common
{
    internal static class Helper
    {
        internal static bool WriteXmlFileToDisk(IRootXmlClass rootXmlObj, XmlSerializerNamespaces nmSpaces,
            string xmlFilepath)
        {
            try
            {
                var serializer = new XmlSerializer(rootXmlObj.GetType());
                Directory.CreateDirectory(Path.GetDirectoryName(xmlFilepath));

                using (TextWriter writer = new StreamWriter(xmlFilepath, true, UpperCaseUTF8.UpperCaseUtf8Encoding))
                    serializer.Serialize(writer, rootXmlObj, nmSpaces);
            }
            catch (Exception e)
            {
                throw new Exception(string.IsNullOrWhiteSpace(e.Message)
                    ? "An unknown error occurred while writing to disk.  Please try again."
                    : e.Message);
            }

            return true;
        }
        public static string GetMD5(string filepath)
        {
            byte[] hash;
            StringBuilder sb = new StringBuilder();

            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filepath))
            {
                hash = md5.ComputeHash(stream);
            }

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public static long GetFilesize(string filepath)
        {
            FileInfo f = new FileInfo(filepath);
            return f.Length;
        }
        internal static string GetNullOrValue(object value)
        {
            return (string.IsNullOrWhiteSpace(value as string)) ? null : value.ToString();
        }

        internal static bool IsTrue(string value)
        {
            return value != null && "Y".Equals(value);
        }

        internal static bool HasValue(object value)
        {
            return value != null;
        }

        /// <summary>
        /// Returns true if passed "Y", otherwise returns false.
        /// </summary>
        /// <param name="value"></param>
        internal static DigitBooleanType GetBoolDigit(string value)
        {
            //On the form:  Item0 = neither box checked, Item1 = yes box checked, Item2 = no box checked, Item3 = both boxes checked (not used)
            //DB representation: Item0 = null, Item1 = "Y", Item2 = "N", Item3 = not used.  bYN is binary.
            return (IsTrue(value) ? DigitBooleanType.True : DigitBooleanType.False);
        }


        /// <summary>
        ///      On the form: Item0 = neither box checked, Item1 = yes box checked, Item2 = no box checked, Item3 = both boxes checked(not used)
        ///      DB representation bYN data type: Item0 = null, Item1 = "Y", Item2 = "N", Item3 = not used.bYN is binary, however if the first box is
        ///      unchecked(value = "N") it is interpreted as blank, to allow coverage to be entered as either yes or no for each indidual month.Otherwise
        ///      a business rule violation is caused server side.
        /// 
        ///      Any element that implements a ShouldSerialize'PropertyName' method  will not be serialized when the value is set to zero.
        ///  </summary>
        internal static DigitCodeType GetDigitCode(string value, bool SerializeElement = false)
        {
            if (SerializeElement)
                //return 1 for true and 2 for false.  Either way element will be serialized.
                return (IsTrue(value) ? DigitCodeType.True : DigitCodeType.No);

            //return 1 for ture and 0 for false.  Element will not be serialized.
            return IsTrue(value) ? DigitCodeType.True : DigitCodeType.False;
        }

        internal static bool IsZeroOrWhitespace(object value)
        {
            if (value == null)
                return true;

            return value.Equals(string.Empty)
                || value.Equals("0")
                || value.Equals(decimal.Zero);
        }

        internal static SqlConnection getUserConnection(object p)
        {
            throw new NotImplementedException();
        }

        internal static SqlConnection get_UserConnection(object p)
        {
            throw new NotImplementedException();
        }

        internal static StateType GetEmployerStateType(object value, bool isTest, bool generateXmlOnSchemaError)
        {
            StateType result;

            //If testing or set to generate xml on schema errors, allow state code to be blank.
            if (string.IsNullOrWhiteSpace(value.ToString()) && !isTest && !generateXmlOnSchemaError)
                throw new Exception("state code in 1094-C employer information can not be blank.");

            Enum.TryParse(value.ToString(),
                ignoreCase: true,
                result: out result);

            return result;
        }

        internal static StateType GetEmployeeStateType(object value, bool isTest, bool generateXmlOnSchemaError)
        {
            StateType result;

            //If testing or set to generate xml on schema errors, allow state code to be blank.
            if (string.IsNullOrWhiteSpace(value.ToString()) && !isTest && !generateXmlOnSchemaError)
                throw new Exception("state code in 1095-C employee information can not be blank.");

            Enum.TryParse(value.ToString(),
                ignoreCase: true,
                result: out result);

            return result;
        }

        internal static decimal GetDecimalOrZero(object value)
        {
            decimal parsedDecimal;
            return decimal.TryParse((string)value, out parsedDecimal) ? parsedDecimal : 0;
        }

        internal static bool IsTrueDigitCode(DigitCodeType digit)
        {
            return digit == DigitCodeType.True;
        }

        internal static bool IsTrueDigitBool(DigitBooleanType boolDigit)
        {
            return boolDigit == DigitBooleanType.True;
        }

        /// <summary>
        /// Sets hardcoded xml namespace aliases and their associated path.  This method may need modified on schema updates.
        /// </summary>
        internal static XmlSerializerNamespaces GetNamespaces()
        {
            var xmlSerializerNamespaces = new XmlSerializerNamespaces();

            xmlSerializerNamespaces.Add("urn", "urn:us:gov:treasury:irs:ext:aca:air:ty16");
            xmlSerializerNamespaces.Add("urn1", "urn:us:gov:treasury:irs:common");
            xmlSerializerNamespaces.Add("urn2", "urn:us:gov:treasury:irs:msg:acabusinessheader");
            xmlSerializerNamespaces.Add("urn3", "urn:us:gov:treasury:irs:msg:acasecurityheader");
            xmlSerializerNamespaces.Add("urn4", "urn:us:gov:treasury:irs:msg:irsacabulkrequesttransmitter");

            return xmlSerializerNamespaces;
        }

        /// <summary>
        /// The xml header encoding element must be in upper case due to IRS spec.  By default it is lower case (utf-8 as opposed to UTF-8).
        /// The Serializer retrieves the string from the WebName field of the UTF8Encoding class, extend UTF8Encoding to call ToUpper on the field.
        /// </summary>
        private class UpperCaseUTF8 : UTF8Encoding
        {
            public static readonly UpperCaseUTF8 UpperCaseUtf8Encoding = new UpperCaseUTF8();

            public override string WebName
            {
                get { return base.WebName.ToUpper(); }
            }
        }

        internal static bool GetBool(string v)
        {
            throw new NotImplementedException();
        }

        internal static SqlConnection get_UserConnection()
        {
            throw new NotImplementedException();
        }
    }
}
