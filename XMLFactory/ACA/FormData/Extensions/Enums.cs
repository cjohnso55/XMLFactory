using System;
using System.Xml.Serialization;

namespace XMLFactory.ACA.FormData
{

    /// <summary>
    /// Digit Code Type102015-08-07Initial VersionType for digit code values, 1 (Yes only), 2 (No only), or 3 (both marked), 0 (Neither)
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Xsd2Code", "4.2.0.31")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:us:gov:treasury:irs:ext:aca:air:ty16")]
    public enum DigitCodeType
    {
        [XmlEnum("0")]
        False,
        [XmlEnum("1")]
        True,
        [XmlEnum("2")]
        No
    }

    /// <summary>
    /// Digit Boolean Type102015-07-14Initial VersionType for digit boolean values. 0= False, 1=True
    /// </summary>
    //[System.CodeDom.Compiler.GeneratedCodeAttribute("Xsd2Code", "4.2.0.31")]
    //[System.SerializableAttribute()]
    //[System.Xml.Serialization.XmlTypeAttribute(Namespace="urn:us:gov:treasury:irs:ext:aca:air:ty16")]
    public enum DigitBooleanType
    {
        [XmlEnum("0")]
        False,
        [XmlEnum("1")]
        True,
    }
}
