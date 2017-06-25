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
    public partial class EmployeeOfferAndCoverageGrpType
    {
        public bool ShouldSeriializeAnnualSafeHarborCd() {
            return !string.IsNullOrWhiteSpace(_annualSafeHarborCd);
        }

        public bool ShouldSeriializeAnnlShrLowestCostMthlyPremAmt() {
            return !string.IsNullOrWhiteSpace(_annlShrLowestCostMthlyPremAmt);
        }

        public bool ShouldSerializeAnnualOfferOfCoverageCd() {
            return !string.IsNullOrEmpty(_annualOfferOfCoverageCd);
        }

        public bool ShouldSerializeMonthlyOfferCoverageGrp() {
            return _monthlyOfferCoverageGrp != null
                   && _monthlyOfferCoverageGrp.ShouldSerialize();
        }

        public bool ShouldSerializeMonthlyShareOfLowestCostMonthlyPremGrp() {
            return _monthlyShareOfLowestCostMonthlyPremGrp != null
                   && _monthlyShareOfLowestCostMonthlyPremGrp.ShouldSerialize();
        }

        public bool ShouldSerializeMonthlySafeHarborGrp() {
            return _monthlySafeHarborGrp != null
                   && _monthlySafeHarborGrp.ShouldSerialize();
        }

        /// <summary>
        /// If any one month has a value, write out the entire group.  The ShouldSerialize 
        /// virutual method of the serializer class only applies to properties.  This method
        /// can be called from the property instance created in the calling object.
        /// </summary>
        public bool ShouldSerialize() {
            return ShouldSerializeAnnualOfferOfCoverageCd()                ||
                   ShouldSerializeMonthlyOfferCoverageGrp()                ||
                   ShouldSerializeMonthlySafeHarborGrp()                   ||
                   ShouldSerializeMonthlyShareOfLowestCostMonthlyPremGrp() ||
                   ShouldSeriializeAnnualSafeHarborCd()                    ||
                   ShouldSeriializeAnnlShrLowestCostMthlyPremAmt();
        }
    }
}