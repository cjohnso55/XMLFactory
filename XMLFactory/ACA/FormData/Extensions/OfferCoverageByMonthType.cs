namespace XMLFactory.ACA.FormData
{
    /// <summary>
    /// Implements the serialization virtual functions for the generated code.
    /// </summary>
    public partial class OfferCoverageByMonthType
    {
        public bool ShouldSerializeJanOfferCd() {
            return !string.IsNullOrWhiteSpace(_janOfferCd);
        }

        public bool ShouldSerializeFebOfferCd() {
            return !string.IsNullOrWhiteSpace(_febOfferCd);
        }

        public bool ShouldSerializeMarOfferCd() {
            return !string.IsNullOrWhiteSpace(_marOfferCd);
        }

        public bool ShouldSerializeAprOfferCd() {
            return !string.IsNullOrWhiteSpace(_aprOfferCd);
        }

        public bool ShouldSerializeMayOfferCd() {
            return !string.IsNullOrWhiteSpace(_mayOfferCd);
        }

        public bool ShouldSerializeJunOfferCd() {
            return !string.IsNullOrWhiteSpace(_junOfferCd);
        }

        public bool ShouldSerializeJulOfferCd() {
            return !string.IsNullOrWhiteSpace(_julOfferCd);
        }

        public bool ShouldSerializeAugOfferCd() {
            return !string.IsNullOrWhiteSpace(_augOfferCd);
        }

        public bool ShouldSerializeSepOfferCd() {
            return !string.IsNullOrWhiteSpace(_sepOfferCd);
        }

        public bool ShouldSerializeOctOfferCd() {
            return !string.IsNullOrWhiteSpace(_octOfferCd);
        }

        public bool ShouldSerializeNovOfferCd() {
            return !string.IsNullOrWhiteSpace(_novOfferCd);
        }

        public bool ShouldSerializeDecOfferCd() {
            return !string.IsNullOrWhiteSpace(_decOfferCd);
        }

        /// <summary>
        /// If any one month has a value, write out the entire group.  The ShouldSerialize 
        /// virutual method of the serializer class only applies to properties.  This method
        /// can be called from the property instance created in the calling object.
        /// </summary>
        public bool ShouldSerialize() {
            return  ShouldSerializeJanOfferCd() ||
                    ShouldSerializeFebOfferCd() ||
                    ShouldSerializeMarOfferCd() ||
                    ShouldSerializeAprOfferCd() ||
                    ShouldSerializeMayOfferCd() ||
                    ShouldSerializeJunOfferCd() ||
                    ShouldSerializeJulOfferCd() ||
                    ShouldSerializeAugOfferCd() ||
                    ShouldSerializeSepOfferCd() ||
                    ShouldSerializeOctOfferCd() ||
                    ShouldSerializeNovOfferCd() ||
                    ShouldSerializeDecOfferCd();
        }
    }
}