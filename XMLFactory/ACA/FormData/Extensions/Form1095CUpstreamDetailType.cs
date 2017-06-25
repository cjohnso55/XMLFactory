namespace XMLFactory.ACA.FormData
{
    public partial class Form1095CUpstreamDetailType
    {
        public bool ShouldSerializeEmployeeOfferAndCoverageGrp() {
            return _employeeOfferAndCoverageGrp.ShouldSerialize();
        }

        public bool ShouldSerializeStartMonthNumberCd() {
            return !string.IsNullOrWhiteSpace(_startMonthNumberCd);
        }

        public bool ShouldSerializeCoveredIndividualGrp() {
            return _coveredIndividualGrp.Count > 0;
        }
    }
}
