using XMLFactory.Common;

namespace XMLFactory.ACA.FormData
{
    /// <summary>
    /// Implements the serialization virtual functions for the generated code.
    /// </summary>
    public partial class ALEMemberInformationGrpType
    {
        public bool ShouldSerializeYearlyALEMemberDetail() {
            return _yearlyALEMemberDetail.ShouldSerialize();
        }

        public bool ShouldSerializeJanALEMonthlyInfoGrp() {
            return _janALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeFebALEMonthlyInfoGrp() {
            return _febALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeMarALEMonthlyInfoGrp() {
            return _marALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeAprALEMonthlyInfoGrp() {
            return _aprALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeMayALEMonthlyInfoGrp() {
            return _mayALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeJunALEMonthlyInfoGrp() {
            return _junALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeJulALEMonthlyInfoGrp() {
            return _julALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeAugALEMonthlyInfoGrp() {
            return _augALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeSeptALEMonthlyInfoGrp() {
            return _septALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeOctALEMonthlyInfoGrp() {
            return _octALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeNovALEMonthlyInfoGrp() {
            return _novALEMonthlyInfoGrp.ShouldSerialize();
        }

        public bool ShouldSerializeDecALEMonthlyInfoGrp() {
            return _decALEMonthlyInfoGrp.ShouldSerialize();
        }

        /// <summary>
        /// Determine if each monthly MEC flag for this instance is true.
        /// </summary>
        /// <returns>True if all months are true false otherwise.</returns>>
        public bool AllMonthlyMECFlagsAreTrue() {
            return Helper.IsTrueDigitCode(_janALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_febALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_marALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_aprALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_mayALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_junALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_julALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_augALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_septALEMonthlyInfoGrp.MinEssentialCvrOffrCd) &&
                   Helper.IsTrueDigitCode(_octALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_novALEMonthlyInfoGrp.MinEssentialCvrOffrCd)  &&
                   Helper.IsTrueDigitCode(_decALEMonthlyInfoGrp.MinEssentialCvrOffrCd);
        }

        /// <summary>
        /// Determine if all monthly aggregate group flag for this instance is true.
        /// </summary>
        /// <returns>True if all months are true false otherwise.</returns>>
        public bool AllMonthlyAggregateGrpFlagsAreTrue() {
            return Helper.IsTrueDigitBool(_janALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_febALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_marALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_aprALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_mayALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_junALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_julALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_augALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_septALEMonthlyInfoGrp.AggregatedGroupInd) &&
                   Helper.IsTrueDigitBool(_octALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_novALEMonthlyInfoGrp.AggregatedGroupInd)  &&
                   Helper.IsTrueDigitBool(_decALEMonthlyInfoGrp.AggregatedGroupInd);
        }
    }
}



