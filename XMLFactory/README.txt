An assembly that will digest and parse employee data and generate an XML document in accordance with the IRS AIR 
specification. For tax year 2016.  For more information and the full specification see here:

https://www.irs.gov/for-tax-pros/software-developers/information-returns/affordable-care-act-information-return-air-program
https://www.irs.gov/pub/irs-pdf/p5165.pdf
https://www.irs.gov/pub/irs-pdf/p5258.pdf
https://www.irs.gov/pub/irs-pdf/p5164.pdf

The schema validation was accomplished using xsd2code++.  Instructions to build the file are located at the top of the 
generated code (CustomAssemblies.XMLFactory.Manifest.TransmitterACAUIBusinessHeader and XMLFactory.ACA.FormData.Form109495CTransmittalUpstreamType classes).

While the database mappings will be different if one were to port this to another system, I've included the sproc that 
I wrote to retrieve from the database I was using.  I may get around to dummying something up in a database project,
and generating some data at some point.

Once the fields are mapped, it should be as simple as updating the two schemas provided to generate the documents
the IRS requires, then fixing any issues found in testing.

I wish no one ther hell of working with government contractors.  If this helps someone, it would be at list the tiniest
bit redeeming.

Craig