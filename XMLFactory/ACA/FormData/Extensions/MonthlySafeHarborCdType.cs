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
    public partial class MonthlySafeHarborCdType
    {
        public bool ShouldSerializeJanSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_janSafeHarborCd);
        }
        public bool ShouldSerializeFebSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_febSafeHarborCd);
        }
        public bool ShouldSerializeMarSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_marSafeHarborCd);
        }
        public bool ShouldSerializeAprSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_aprSafeHarborCd);
        }
        public bool ShouldSerializeMaySafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_maySafeHarborCd);
        }

        public bool ShouldSerializeJunSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_junSafeHarborCd);
        }
        public bool ShouldSerializeJulSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_julSafeHarborCd);
        }
        public bool ShouldSerializeAugSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_augSafeHarborCd);
        }
        public bool ShouldSerializeSepSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_sepSafeHarborCd);
        }
        public bool ShouldSerializeOctSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_octSafeHarborCd);
        }
        public bool ShouldSerializeNovSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_novSafeHarborCd);
        }
        public bool ShouldSerializeDecSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_decSafeHarborCd);
        }

        /// <summary>
        /// If any one month has a value, write out the entire group.  The ShouldSerialize 
        /// virtual method of the serializer class only applies to properties.  This method
        /// can be called from the property instance created in the calling object.  
        /// </summary>
        public bool ShouldSerialize() {
            return
                ShouldSerializeJanSafeHarborCd() ||
                ShouldSerializeFebSafeHarborCd() ||
                ShouldSerializeMarSafeHarborCd() ||
                ShouldSerializeAprSafeHarborCd() ||
                ShouldSerializeMaySafeHarborCd() ||
                ShouldSerializeJunSafeHarborCd() ||
                ShouldSerializeJulSafeHarborCd() ||
                ShouldSerializeAugSafeHarborCd() ||
                ShouldSerializeOctSafeHarborCd() ||
                ShouldSerializeSepSafeHarborCd() ||
                ShouldSerializeNovSafeHarborCd() ||
                ShouldSerializeDecSafeHarborCd(); 
        }
    }
}

