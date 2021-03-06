﻿using System;

namespace XMLFactory.Interfaces
{
    /// <summary>
    /// Interface to reference various Xml Root Classes.
    /// Also taking advantage of the method generated by the utility xsd2code
    /// to save itself to file.
    /// So, we do not have to have a separate utility to generate xml file.
    /// </summary>
    public interface IRootXmlClass
    {
        bool SaveToFile(string fileName, out Exception exception);
    }
}
