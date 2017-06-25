namespace XMLFactory.ACA.FormData
{
    /// <summary>
    /// Implements the serialization virtual functions for the generated code.
    /// </summary>
    public partial class EmployerCoveredIndividualType
    {
        /// <summary>
        /// If any one month has a value, write out the entire group.  The ShouldSerialize 
        /// virutual method of the serializer class only applies to properties.  This method
        /// can be called from the property instance created in the calling object.
        /// </summary>
        public bool ShouldSerializeCoveredIndividualAnnualInd() {
            return _coveredIndividualAnnualInd != DigitBooleanType.False;
        }

        /// <summary>
        /// If any one month has a value, write out the entire group.  The ShouldSerialize 
        /// virutual method of the serializer class only applies to properties.  This method
        /// can be called from the property instance created in the calling object.
        /// </summary>
        public bool ShouldSerializeCoveredIndividualMonthlyIndGrp() {
            return _coveredIndividualMonthlyIndGrp.ShouldSerialize();
        }
    }
}