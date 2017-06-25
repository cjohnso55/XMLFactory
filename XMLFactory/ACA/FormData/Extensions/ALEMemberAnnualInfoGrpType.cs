
namespace XMLFactory.ACA.FormData
{
    /// <summary>
    /// Implements the serialization virtual functions for the generated code.
    /// </summary>
    public partial class ALEMemberAnnualInfoGrpType
    {
        public bool ShouldSerializeMinEssentialCvrOffrCd() {
            return (_minEssentialCvrOffrCd != DigitCodeType.False);
        }
        public bool ShouldSerializeAggregatedGroupInd() {
            return (_aggregatedGroupInd != DigitBooleanType.False);
        }
        public bool ShouldSerializeALEMemberFTECnt() {
            return (!string.IsNullOrWhiteSpace(_aLEMemberFTECnt));
        }
        public bool ShouldSerializeTotalEmployeeCnt() {
            return (!string.IsNullOrWhiteSpace(_totalEmployeeCnt));
        }
        public bool ShouldSerializeALESect4980HTrnstReliefCd() {
            return (!string.IsNullOrWhiteSpace(_aLESect4980HTrnstReliefCd));
        }
        /// <summary>
        /// This method is not provided.  Used to omit complex elements from xml as requred by IRS.
        /// </summary>
        /// <returns></returns>
        public bool ShouldSerialize() {
            return
                ShouldSerializeAggregatedGroupInd()        ||
                ShouldSerializeALEMemberFTECnt()           ||
                ShouldSerializeMinEssentialCvrOffrCd()     ||
                ShouldSerializeALESect4980HTrnstReliefCd() ||
                ShouldSerializeTotalEmployeeCnt();
        }
    }
}
