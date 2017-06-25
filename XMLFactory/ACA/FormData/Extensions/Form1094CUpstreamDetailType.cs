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
    public partial class Form1094CUpstreamDetailType
    {
        public bool ShouldSerializeAggregatedGroupMemberCd() {
            return (_aggregatedGroupMemberCd != DigitCodeType.False);
        }
        public bool ShouldSerializeQualifyingOfferMethodInd() {
            return (_qualifyingOfferMethodInd != DigitBooleanType.False);
        }
        public bool ShouldSerializeSection4980HReliefInd() {
            return (_section4980HReliefInd != DigitBooleanType.False);
        }
        public bool ShouldSerializeNinetyEightPctOfferMethodInd() {
            return (_ninetyEightPctOfferMethodInd != DigitBooleanType.False);
        }
        public bool ShouldSerializeAuthoritativeTransmittalInd() {
            return (_authoritativeTransmittalInd != DigitBooleanType.False);
        }
    }
}
