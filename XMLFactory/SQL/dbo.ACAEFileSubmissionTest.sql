IF  EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ACAEFileSubmissionTest]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[ACAEFileSubmissionTest]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE proc [dbo].[ACAEFileSubmissionTest]
/****************************************************************************
* Created By:  Craig Johnson 	
*				
*
* USAGE:  Generate the 2015 IRS AATS Test Submission 3 output for efile. 
*
* INPUT PARAMETERS:  None
*
* Return:	Result sets needed by PR ACA E-File process to generate 
*			the AIR Manifest and Form Data Files required for AATS 
*			testing.
*
*****************************************************************************/
(@PRCo bCompany,@TaxYear char(4))
as
set nocount on

--1094 Header
DECLARE @1094CHeader Table
(
PRCo tinyint, TaxYear char(4), ALEName varchar(60) null, ALEEIN varchar(20) null, ALEAddress varchar(60) null,
ALECity varchar(30) null, ALEState varchar(4) null, ALECountry varchar(2) null, ALEPostalCode varchar(12) null,
ALEContactName varchar(60) null, ALEContactPhone varchar(20) null, AuthoritativeTransmittal char(1) null,
IsMemberOfALEGroup char(1) null, QualOfferMethod char(1) null, QualOfferMethodTransRelief char(1) null,
Sec4980HTransRelief char(1) null, NinetyEightPctOffer char(1) null, Notes varchar(max) null, 
UniqueAttchID uniqueidentifier null, KeyID bigint null, USZipCd varchar(7) null, USZipExtensionCd varchar(5) null,
formattedEIN varchar(20) null, formattedPhoneNum varchar(20), businessNameControlTxt varchar(60) null, 
ALEContactFirstName varchar(60) null, ALEContactLastName varchar(60) null, Jan_MECFlag char(1) null,
Feb_MECFlag char(1) null, Mar_MECFlag char(1) null, Apr_MECFlag char(1) null, May_MECFlag char(1) null, 
Jun_MECFlag char(1) null, Jul_MECFlag char(1) null, Aug_MECFlag char(1) null, Sep_MECFlag char(1) null, 
Oct_MECFlag char(1) null, Nov_MECFlag char(1) null, Dec_MECFlag char(1) null, Yearly_MECFlag char(1) null, 
Jan_FullTimeEmplCount int null, Feb_FullTimeEmplCount int null, Mar_FullTimeEmplCount int null, 
Apr_FullTimeEmplCount int null, May_FullTimeEmplCount int null, Jun_FullTimeEmplCount int null, 
Jul_FullTimeEmplCount int null, Aug_FullTimeEmplCount int null, Sep_FullTimeEmplCount int null, 
Oct_FullTimeEmplCount int null, Nov_FullTimeEmplCount int null, Dec_FullTimeEmplCount int null, 
Yearly_FullTimeEmplCount int null, Jan_TotalEmplCount int null, Feb_TotalEmplCount int null, 
Mar_TotalEmplCount int null, Apr_TotalEmplCount int null, May_TotalEmplCount int null, 
Jun_TotalEmplCount int null, Jul_TotalEmplCount int null, Aug_TotalEmplCount int null, 
Sep_TotalEmplCount int null, Oct_TotalEmplCount int null, Nov_TotalEmplCount int null, 
Dec_TotalEmplCount int null, Yearly_TotalEmplCount int null, Jan_AggregatedGroupFlag char(1) null,
Feb_AggregatedGroupFlag char(1) null, Mar_AggregatedGroupFlag char(1) null, Apr_AggregatedGroupFlag char(1) null,
May_AggregatedGroupFlag char(1) null, Jun_AggregatedGroupFlag char(1) null, Jul_AggregatedGroupFlag char(1) null,
Aug_AggregatedGroupFlag char(1) null, Sep_AggregatedGroupFlag char(1) null, Oct_AggregatedGroupFlag char(1) null,
Nov_AggregatedGroupFlag char(1) null, Dec_AggregatedGroupFlag char(1) null, Yearly_AggregatedGroupFlag char(1) null,
Jan_TransReliefFlag char(1) null, Feb_TransReliefFlag char(1) null, Mar_TransReliefFlag char(1) null, Apr_TransReliefFlag char(1) null,
May_TransReliefFlag char(1) null, Jun_TransReliefFlag char(1) null, Jul_TransReliefFlag char(1) null, Aug_TransReliefFlag char(1) null,
Sep_TransReliefFlag char(1) null, Oct_TransReliefFlag char(1) null, Nov_TransReliefFlag char(1) null, Dec_TransReliefFlag char(1) null,
Yearly_TransReliefFlag char(1) null, CorrectionYN char(1) null)

INSERT @1094CHeader (PRCo, TaxYear, ALEName, ALEEIN, ALEAddress, ALECity, ALEState, ALECountry, ALEPostalCode,
ALEContactName, ALEContactPhone, AuthoritativeTransmittal, IsMemberOfALEGroup, 
QualOfferMethod, QualOfferMethodTransRelief, Sec4980HTransRelief, NinetyEightPctOffer, Notes, 
UniqueAttchID, KeyID, USZipCd, USZipExtensionCd, formattedEIN, formattedPhoneNum, businessNameControlTxt, 
ALEContactFirstName, ALEContactLastName, 

Jan_MECFlag, Feb_MECFlag, Mar_MECFlag, Apr_MECFlag, May_MECFlag, Jun_MECFlag, 
Jul_MECFlag, Aug_MECFlag, Sep_MECFlag, Oct_MECFlag, Nov_MECFlag, Dec_MECFlag, Yearly_MECFlag, 

Jan_FullTimeEmplCount, Feb_FullTimeEmplCount, Mar_FullTimeEmplCount, Apr_FullTimeEmplCount, 
May_FullTimeEmplCount, Jun_FullTimeEmplCount, Jul_FullTimeEmplCount, Aug_FullTimeEmplCount, 
Sep_FullTimeEmplCount, Oct_FullTimeEmplCount, Nov_FullTimeEmplCount, Dec_FullTimeEmplCount, Yearly_FullTimeEmplCount, 

Jan_TotalEmplCount, Feb_TotalEmplCount, Mar_TotalEmplCount, Apr_TotalEmplCount, May_TotalEmplCount, Jun_TotalEmplCount,
Jul_TotalEmplCount, Aug_TotalEmplCount, Sep_TotalEmplCount, Oct_TotalEmplCount, Nov_TotalEmplCount, Dec_TotalEmplCount,
Yearly_TotalEmplCount, 

Jan_AggregatedGroupFlag, Feb_AggregatedGroupFlag, Mar_AggregatedGroupFlag, Apr_AggregatedGroupFlag,
May_AggregatedGroupFlag, Jun_AggregatedGroupFlag, Jul_AggregatedGroupFlag, Aug_AggregatedGroupFlag, 
Sep_AggregatedGroupFlag, Oct_AggregatedGroupFlag,Nov_AggregatedGroupFlag, Dec_AggregatedGroupFlag, 
Yearly_AggregatedGroupFlag,

Jan_TransReliefFlag, Feb_TransReliefFlag, Mar_TransReliefFlag, Apr_TransReliefFlag,
May_TransReliefFlag, Jun_TransReliefFlag, Jul_TransReliefFlag, Aug_TransReliefFlag,
Sep_TransReliefFlag, Oct_TransReliefFlag, Nov_TransReliefFlag, Dec_TransReliefFlag,
Yearly_TransReliefFlag, CorrectionYN)

Values (@PRCo, @TaxYear, 'Selitestthree', '000000301', '6689 Willow Court', 'Beverly Hills', 'CA', 'US', '90211',
'Rose Lincoln', '555-987-6543', 'Y', 'Y', 'N', 'N', 'Y', 'N', null, null, 1, '90211', null, '000000301',
'5559876534', 'SELI', 'Rose', 'Lincoln', 

/*Monthly MEF Offer */ 'N', 'N', 'N', 'N', 'N', 'Y', 'Y', 'Y', 'Y', 'Y', 'Y', 'Y', 'N',
/*Monthly Counts Full Time*/ 312, 312, 315, 320, 322, 325, 329, 333, 341, 344, 361, 372, 0,
/*Monthly Counts Total Emp*/ 351, 352, 358, 365, 369, 376, 372, 369, 366, 363, 377, 385, 0,
/*Monthly Agg Group */ 'N','N','N','N','N','N','N','N','N','N','N','N','Y',
/*Monthly Trans Relief*/ null, null, null, null, null, null, null, null, null, null, null, null, 'B'
,'N') 

DECLARE @1095Header TABLE(
PRCo tinyint null, TaxYear char(4) null, Employee int null, LastName varchar(30) null, FirstName varchar(30) null, 
formattedSSN varchar(11) null, [Address] varchar(60) null, City varchar(30) null, [State] varchar(4) null, Zip varchar(12) null,
Country char(2) null, IsFullTime char(1) null, SelfInsCoverage char(1), OfferCoverageAll12Mths char(2) null,
EmployeeShareAll12Mths numeric(12,2) null, Section4980HAll12Mths char(2) null, Notes varchar(max) null, 
UniqueAttchID uniqueidentifier null, KeyID bigint, TypeOVC char(1), PrintYN char(1), TransmissionID int null,
RecordID int null, OriginalRecordID int null, NewTransmissionID int null,
NewRecordID bigint null, USZipCd varchar(10), USZipExtensionCd varchar(4) null, formamttedSSN varchar(9) null, 
personNameControlTxt varchar(60), Jan_CoverageOffer char(2) null, Feb_CoverageOffer char(2) null, 
Mar_CoverageOffer char(2) null, Apr_CoverageOffer char(2) null, May_CoverageOffer char(2) null, 
Jun_CoverageOffer char(2) null, Jul_CoverageOffer char(2) null, Aug_CoverageOffer char(2) null, 
Sep_CoverageOffer char(2) null, Oct_CoverageOffer char(2) null, Nov_CoverageOffer char(2) null, 
Dec_CoverageOffer char(2) null, Jan_SafeHarbor char(2) null, Feb_SafeHarbor char(2) null, 
Mar_SafeHarbor char(2) null, Apr_SafeHarbor char(2) null, May_SafeHarbor char(2) null, Jun_SafeHarbor char(2) null, 
Jul_SafeHarbor char(2) null, Aug_SafeHarbor char(2) null, Sep_SafeHarbor char(2) null,
Oct_SafeHarbor char(2) null, Nov_SafeHarbor char(2) null, Dec_SafeHarbor char(2) null, 
Jan_ShareAmount numeric(12,2) null, Feb_ShareAmount numeric(12,2) null, Mar_ShareAmount numeric(12,2) null, 
Apr_ShareAmount numeric(12,2) null, May_ShareAmount numeric(12,2) null, Jun_ShareAmount numeric(12,2) null,
Jul_ShareAmount numeric(12,2) null, Aug_ShareAmount numeric(12,2) null, Sep_ShareAmount numeric(12,2) null, 
Oct_ShareAmount numeric(12,2) null, Nov_ShareAmount numeric(12,2) null, Dec_ShareAmount numeric(12,2) null
)

/*Teresa Southern*/
INSERT @1095Header (
/*Part 1 and Misc*/
PRCo, TaxYear, Employee, LastName, FirstName, formattedSSN, [Address], City, [State], Zip, Country, 
IsFullTime, SelfInsCoverage, Notes,
UniqueAttchID, KeyID, TypeOVC, PrintYN, TransmissionID,
RecordID, OriginalRecordID, NewTransmissionID,
NewRecordID, USZipCd, USZipExtensionCd, formamttedSSN, personNameControlTxt, 

/*All 12 Month Offers*/
OfferCoverageAll12Mths, EmployeeShareAll12Mths, Section4980HAll12Mths,  
/*Monthly Offer*/
Jan_CoverageOffer, Feb_CoverageOffer, Mar_CoverageOffer, 
Apr_CoverageOffer, May_CoverageOffer, Jun_CoverageOffer, 
Jul_CoverageOffer, Aug_CoverageOffer, Sep_CoverageOffer, 
Oct_CoverageOffer, Nov_CoverageOffer, Dec_CoverageOffer, 
/*Section 4980H Safe Harbor*/
Jan_SafeHarbor, Feb_SafeHarbor, Mar_SafeHarbor, Apr_SafeHarbor, May_SafeHarbor, Jun_SafeHarbor, 
Jul_SafeHarbor, Aug_SafeHarbor, Sep_SafeHarbor, Oct_SafeHarbor, Nov_SafeHarbor, Dec_SafeHarbor, 
/*Self Only Share*/ 
Jan_ShareAmount, Feb_ShareAmount, Mar_ShareAmount, Apr_ShareAmount, May_ShareAmount, Jun_ShareAmount,
Jul_ShareAmount, Aug_ShareAmount, Sep_ShareAmount, Oct_ShareAmount, Nov_ShareAmount, Dec_ShareAmount)

VALUES (
/*Part 1 and Misc*/
@PRCo, @TaxYear, 1, 'Southern', 'Teresa', '000000350', '342 Ash Avenue', 'Seattle', 'WA', '98104', 'US',
'Y', 'N', null, 
null, 1, 'O', 'N', null,
null, null, 1,
1, '98104', null, '000000350', 'SOUT',
 
/*All 12 Month Offers*/
null, null, null,
/*Monthly Offer*/
'1H','1H','1H',
'1H','1H','1E',
'1E','1E','1E',
'1E','1E','1E',
/*Section 4980H Safe Harbor*/
null, null, null, null, null, '2C', 
'2C', '2C', '2C', '2C', '2C', '2C', 
/*Self Only Share*/ 
null, null, null, null, null, 139,
139.00, 139.00, 139.00, 139.00, 139.00, 139.00)

/*Wanda Souter*/
INSERT @1095Header (
/*Part 1 and Misc*/
PRCo, TaxYear, Employee, LastName, FirstName, formattedSSN, [Address], City, [State], Zip, Country, 
IsFullTime, SelfInsCoverage, Notes,
UniqueAttchID, KeyID, TypeOVC, PrintYN, TransmissionID,
RecordID, OriginalRecordID, NewTransmissionID,
NewRecordID, USZipCd, USZipExtensionCd, formamttedSSN, personNameControlTxt, 

/*All 12 Month Offers*/
OfferCoverageAll12Mths, EmployeeShareAll12Mths, Section4980HAll12Mths,  
/*Monthly Offer*/
Jan_CoverageOffer, Feb_CoverageOffer, Mar_CoverageOffer, 
Apr_CoverageOffer, May_CoverageOffer, Jun_CoverageOffer, 
Jul_CoverageOffer, Aug_CoverageOffer, Sep_CoverageOffer, 
Oct_CoverageOffer, Nov_CoverageOffer, Dec_CoverageOffer, 
/*Section 4980H Safe Harbor*/
Jan_SafeHarbor, Feb_SafeHarbor, Mar_SafeHarbor, Apr_SafeHarbor, May_SafeHarbor, Jun_SafeHarbor, 
Jul_SafeHarbor, Aug_SafeHarbor, Sep_SafeHarbor, Oct_SafeHarbor, Nov_SafeHarbor, Dec_SafeHarbor, 
/*Self Only Share*/ 
Jan_ShareAmount, Feb_ShareAmount, Mar_ShareAmount, Apr_ShareAmount, May_ShareAmount, Jun_ShareAmount,
Jul_ShareAmount, Aug_ShareAmount, Sep_ShareAmount, Oct_ShareAmount, Nov_ShareAmount, Dec_ShareAmount)

VALUES (
/*Part 1 and Misc*/
@PRCo, @TaxYear, 2, 'Souter', 'Wanda', '000000351', '46789 Aspen Avenue', 'Wasco', 'OR', '97065', 'US',
'Y', 'N', null, 
null, 2, 'O', 'N', null,
null, null, 1,
2, '97065', null, '000000351', 'SOUT',
 
/*All 12 Month Offers*/
null, null, null,
/*Monthly Offer*/
'1H','1H','1H',
'1H','1H','1E',
'1E','1E','1E',
'1E','1E','1E',
/*Section 4980H Safe Harbor*/
null, null, null, null, null, '2C', 
'2C', '2C', '2C', '2C', '2C', '2C', 
/*Self Only Share*/ 
null, null, null, null, null, 148,
148.00, 148.00, 148.00, 148.00, 148.00, 148.00)

/*Arthur Soutane*/
INSERT @1095Header (
/*Part 1 and Misc*/
PRCo, TaxYear, Employee, LastName, FirstName, formattedSSN, [Address], City, [State], Zip, Country, 
IsFullTime, SelfInsCoverage, Notes,
UniqueAttchID, KeyID, TypeOVC, PrintYN, TransmissionID,
RecordID, OriginalRecordID, NewTransmissionID,
NewRecordID, USZipCd, USZipExtensionCd, formamttedSSN, personNameControlTxt, 

/*All 12 Month Offers*/
OfferCoverageAll12Mths, EmployeeShareAll12Mths, Section4980HAll12Mths,  
/*Monthly Offer*/
Jan_CoverageOffer, Feb_CoverageOffer, Mar_CoverageOffer, 
Apr_CoverageOffer, May_CoverageOffer, Jun_CoverageOffer, 
Jul_CoverageOffer, Aug_CoverageOffer, Sep_CoverageOffer, 
Oct_CoverageOffer, Nov_CoverageOffer, Dec_CoverageOffer, 
/*Section 4980H Safe Harbor*/
Jan_SafeHarbor, Feb_SafeHarbor, Mar_SafeHarbor, Apr_SafeHarbor, May_SafeHarbor, Jun_SafeHarbor, 
Jul_SafeHarbor, Aug_SafeHarbor, Sep_SafeHarbor, Oct_SafeHarbor, Nov_SafeHarbor, Dec_SafeHarbor, 
/*Self Only Share*/ 
Jan_ShareAmount, Feb_ShareAmount, Mar_ShareAmount, Apr_ShareAmount, May_ShareAmount, Jun_ShareAmount,
Jul_ShareAmount, Aug_ShareAmount, Sep_ShareAmount, Oct_ShareAmount, Nov_ShareAmount, Dec_ShareAmount)

VALUES (
/*Part 1 and Misc*/
@PRCo, @TaxYear, 3, 'Soutane', 'Arthur', '000000355', '4375 Alder Lane', 'Kent', 'WA', '98089', 'US',
'Y', 'N', null, 
null, 3, 'O', 'N', null,
null, null, 1,
3, '98089', null, '000000355', 'SOUT',
 
/*All 12 Month Offers*/
null, null, null,
/*Monthly Offer*/
'1H','1H','1H',
'1H','1H','1C',
'1C','1C','1C',
'1C','1C','1C',
/*Section 4980H Safe Harbor*/
null, null, null, null, null, '2C', 
'2C', '2C', '2C', '2C', '2C', '2C', 
/*Self Only Share*/ 
null, null, null, null, null, 148,
145.00, 145.00, 145.00, 145.00, 145.00, 145.00)
 
DECLARE @1095Covered TABLE(
PRCo tinyint, TaxYear varchar(4), Employee int, Seq int, DepPRCo tinyint null, DepEmployee int, 
LastName varchar(30), FirstName varchar(30), SSN varchar(11), DOB smalldatetime null, CoveredAll12Months char(1) null,
Jan char(1) null, Feb char(1) null, Mar char(1) null, June char(1) null, Aug char(1) null,
Sept char(1) null, Oct char(1) null, Nov char(1) null, [Dec] char(1) null, UniqueAttchID uniqueidentifier null, 
KeyID bigint null, DependentID int null, formattedSSN varchar(9), personNameControlTxt varchar(60), formattedDOB varchar(10)
)

DECLARE @1094COtherALEMembers TABLE(
PRCo tinyint, TaxYear char(4), Seq int, MemberPRCo tinyint null, MemberName varchar(60), EIN varchar(20), 
ALERank int null, UniqueAttchID uniqueidentifier null, KeyID bigint null, memberNameControlTxt varchar(60) null, 
formattedEIN varchar(9) null , TotalForm1095CALEMemberCnt int null
)

INSERT @1094COtherALEMembers
(PRCo, TaxYear, Seq, MemberPRCo, MemberName, EIN, ALERank, UniqueAttchID, KeyID, memberNameControlTxt, formattedEIN, TotalForm1095CALEMemberCnt)
VALUES
(@PRCo, @TaxYear, 1, null, 'Selitestthree Subsidiary One', '000000302', 1, null, 1, 'SELI', '000000302', 5)

INSERT @1094COtherALEMembers
(PRCo, TaxYear, Seq, MemberPRCo, MemberName, EIN, ALERank, UniqueAttchID, KeyID, memberNameControlTxt, formattedEIN, TotalForm1095CALEMemberCnt)
VALUES
(@PRCo, @TaxYear, 2, null, 'Selitestthree Subsidiary Two', '000000303', 2, null, 2, 'SELI', '000000303', 5)

INSERT @1094COtherALEMembers
(PRCo, TaxYear, Seq, MemberPRCo, MemberName, EIN, ALERank, UniqueAttchID, KeyID, memberNameControlTxt, formattedEIN, TotalForm1095CALEMemberCnt)
VALUES
(@PRCo, @TaxYear, 3, null, 'Selitestthree Subsidiary Three', '000000304', 3, null, 3, 'SELI', '000000304', 5)

INSERT @1094COtherALEMembers
(PRCo, TaxYear, Seq, MemberPRCo, MemberName, EIN, ALERank, UniqueAttchID, KeyID, memberNameControlTxt, formattedEIN, TotalForm1095CALEMemberCnt)
VALUES
(@PRCo, @TaxYear, 4, null, 'Selitestthree Subsidiary Four', '000000305', 4, null, 4, 'SELI', '000000305', 5)


DECLARE @transtable1 TABLE (
PRCo tinyint, TaxYear char(4), TransmissionID int, CTransmissionID int null, TransmissionType char(1), 
DateGenerated smalldatetime null, UUID uniqueidentifier null, TCC varchar(10) null, ReceiptID varchar(20) null, 
TransmissionStatus char(2) null, UniqueAttchID uniqueidentifier null, KeyID bigint null
)

DECLARE @transtable2 TABLE (
PRCo tinyint, TaxYear char(4), TransmissionID int, CTransmissionID int null, TransmissionType char(1), 
DateGenerated smalldatetime null, UUID uniqueidentifier null, TCC varchar(10) null, ReceiptID varchar(20) null, 
TransmissionStatus char(2) null, UniqueAttchID uniqueidentifier null, KeyID bigint null
)

SELECT * FROM @1094CHeader

SELECT * FROM @1095Header

SELECT * FROM @1095Covered

SELECT * FROM @1094COtherALEMembers

SELECT * FROM @transtable1

SELECT * FROM @transtable2

vspexit:

GO


