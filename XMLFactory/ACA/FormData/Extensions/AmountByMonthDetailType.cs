namespace XMLFactory.ACA.FormData
{
    /// <summary>
    /// Implements the serialization virtual functions for the generated code.  This was implemented to accomodate
    /// omitting serialazation dependent on nested classes being populated.  Had the IRS done something more sane
    /// this wouldn't be here.  However the specification forces removal of parent elements if child elements are 
    /// null, there can be multiple levels of nesting, and the built in .NET mechanisms to do this fail in that case.
    /// The rules are also not intuitive, with some blank elements required and some not.
    /// 
    /// If one of these is incorrect the entire file will be rejected.  If this happens in production buy me 
    /// a big bottle of whiskey please.  Taking a vacation.
    /// </summary>
    public partial class AmountByMonthDetailType
    {
        public bool ShouldSerializeJanuaryAmt() {
            return !string.IsNullOrWhiteSpace(_januaryAmt);
        }
        public bool ShouldSerializeFebruaryAmt() {
            return !string.IsNullOrWhiteSpace(_februaryAmt);
        }
        public bool ShouldSerializeMarchAmt() {
            return !string.IsNullOrWhiteSpace(_marchAmt);
        }
        public bool ShouldSerializeAprilAmt() {
            return !string.IsNullOrWhiteSpace(_aprilAmt);
        }
        public bool ShouldSerializeMayAmt() {
            return !string.IsNullOrWhiteSpace(_mayAmt);
        }
        public bool ShouldSerializeJuneAmt() {
            return !string.IsNullOrWhiteSpace(_juneAmt);
        }
        public bool ShouldSerializeJulyAmt() {
            return !string.IsNullOrWhiteSpace(_julyAmt);
        }
        public bool ShouldSerializeAugustAmt() {
            return !string.IsNullOrWhiteSpace(_augustAmt);
        }
        public bool ShouldSerializeSeptemberAmt() {
            return !string.IsNullOrWhiteSpace(_septemberAmt);
        }
        public bool ShouldSerializeOctoberAmt() {
            return !string.IsNullOrWhiteSpace(_octoberAmt);
        }
        public bool ShouldSerializeNovemberAmt() {
            return !string.IsNullOrWhiteSpace(_novemberAmt);
        }
        public bool ShouldSerializeDecemberAmt() {
            return !string.IsNullOrWhiteSpace(_decemberAmt);
        }

        /// <summary>
        /// If any one month has a value, write out the entire group.  The ShouldSerialize 
        /// virutual method of the serializer class only applies to properties.  This method
        /// can be called from the property instance created in the calling object.
        /// </summary>             
        public bool ShouldSerialize() {
            return
                ShouldSerializeJanuaryAmt()   ||
                ShouldSerializeFebruaryAmt()  ||
                ShouldSerializeMarchAmt()     ||
                ShouldSerializeAprilAmt()     ||
                ShouldSerializeMayAmt()       ||
                ShouldSerializeJuneAmt()      ||
                ShouldSerializeJulyAmt()      ||
                ShouldSerializeAugustAmt()    ||
                ShouldSerializeSeptemberAmt() ||
                ShouldSerializeOctoberAmt()   ||
                ShouldSerializeNovemberAmt()  ||
                ShouldSerializeDecemberAmt();
        }
    }
}