namespace XMLFactory.ACA.FormData
{
    /// <summary>
    /// Implements the serialization virtual functions for the generated code.
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
