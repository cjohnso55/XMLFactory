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
    public partial class MonthIndGrpType 
    {
        public bool ShouldSerializeJanuaryInd() {
            return _januaryInd != DigitBooleanType.False;
        }
        public bool ShouldSerializeFebruaryInd() {
            return _februaryInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeMarchInd() {
            return _marchInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeAprilInd() {
            return _aprilInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeMayInd() {
            return _mayInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeJuneInd() {
            return _juneInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeJulyInd() {
            return _julyInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeAugustInd() {
            return _augustInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeSeptemberInd() {
            return _septemberInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeOctoberInd() {
            return _octoberInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeNovemberInd() {
            return _novemberInd != DigitBooleanType.False;
        }

        public bool ShouldSerializeDecemberInd() {
            return _decemberInd != DigitBooleanType.False;
        }

        //public List<DigitBooleanType> flagValues { get; set; }
        //public List <string> flagValueStrings { get; }
        //public List <bool>   
        /// <summary>
        /// If any one month has a value, write out the entire group.The ShouldSerialize 
        /// virutual method of the serializer class only applies to properties.This method
        /// can be called from the property instance created in the calling object.
        /// </summary>             
        public bool ShouldSerialize() {
            return
                ShouldSerializeJanuaryInd()   ||
                ShouldSerializeFebruaryInd()  ||
                ShouldSerializeMarchInd()     ||
                ShouldSerializeAprilInd()     ||
                ShouldSerializeMayInd()       ||
                ShouldSerializeJuneInd()      ||
                ShouldSerializeJulyInd()      ||
                ShouldSerializeAugustInd()    ||
                ShouldSerializeSeptemberInd() ||
                ShouldSerializeOctoberInd()   ||
                ShouldSerializeNovemberInd()  ||
                ShouldSerializeDecemberInd();
        }
    }
}