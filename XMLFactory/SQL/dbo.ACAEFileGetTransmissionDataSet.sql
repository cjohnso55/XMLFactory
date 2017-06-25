IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ACAEFileGetTransmissionDataSet]') AND type in (N'P', N'PC'))  
DROP PROCEDURE [dbo].[ACAEFileGetTransmissionDataSet];
GO
SET ANSI_NULLS ON 
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROC [dbo].[ACAEFileGetTransmissionDataSet] 
/****************************************************************************
* Created By:  Craig Johnson
* Modified By:	
*				
*
* USAGE:  Denormalize ACA employee data needed to generate AIR transmission.  This data is returned to the
*         XmlFactory.ACA.FormData.FormDataExporter object.
*****************************************************************************/
(@PRCo bCompany, 
 @TaxYear char(4), 
 @TransmissionType char(2) = null,
 @ErrorMessage varchar(255) output) 

AS


Declare @PR1094HeaderCount      int = 0,
        @PR1095HeaderCount      int = 0,
	    @PreviousTransmissionID int,
		@TransmissionID         int,
		@TransmissionDate       datetime = GetDate(),
		@IsReplacement          bit = 0,
	    @TransmissionStatus     char(2)  = (Case @TransmissionType When 'O' Then 'PD'
												                   When 'R' Then 'PD'
													               When 'C' Then 'PD' End)


Begin Try

	Set @PreviousTransmissionID = (Select IsNull(max(hist.TransmissionID), 0)
									    from dbo.PRACAeFileTransmissionHistory hist
										where hist.TaxYear = @TaxYear
										    and hist.PRCo    = @PRCo);

	Set @TransmissionID = @PreviousTransmissionID + 1

	--Replacements that received a ReceiptID are handled like originals
	--with the exception the TransmissionID must not be null.
	If (@TransmissionType = 'R')
	BEGIN
		Set @TransmissionType = 'O';
		Set @IsReplacement = 1;
	END


	--Denormalize/Pivot months to columns in order to get a one to one relationship between PR1094Header and the associated monthly flags in PR1094ALEMemberInfo.
	--To make it easier to digest on the client side, Trying to make sure the data is flattened as much as possible.
	;with MECOfferIndicators as
	(  select * from
	   (  select PRCo,
				 TaxYear,
				 LineDesc,
				 LTRIM(RTRIM(MECOfferIndicator)) as MECOfferIndicator
			  from dbo.PR1094ALEMemberInfo
		) x pivot( max(MECOfferIndicator) 
			  for LineDesc in (Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec], [All 12 Months])) MECOffers
	),
	FullTimeEmployeeCounts as
	(  select * from
	   (  select PRCo,
				 TaxYear,
				 LineDesc,
				 FullTimeEmplCount
			  from dbo.PR1094ALEMemberInfo
		) x pivot( max(FullTimeEmplCount) 
			  for LineDesc in (Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec], [All 12 Months])) FTEmplCounts
	),
	TotalEmployeeCounts as
	(  select * from
	   (  select PRCo,
				 TaxYear,
				 LineDesc,
				 TotalEmplCount
			  from dbo.PR1094ALEMemberInfo
		) x pivot( max(TotalEmplCount) 
			  for LineDesc in (Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec], [All 12 Months])) TotalEmplCounts
	),
	AggregatedGroupIndicators as
	(  select * from
	   (  select PRCo,
				 TaxYear,
				 LineDesc,
				 LTRIM(RTRIM(AggregatedGroupIndicator)) as AggregatedGroupIndicator
			  from dbo.PR1094ALEMemberInfo
		) x pivot( max(AggregatedGroupIndicator) 
			  for LineDesc in (Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec], [All 12 Months])) AggregatedGroupIndicator
	),
	Sec4980HTransReliefFlags as
	(  select * from
	   (  select PRCo,
				 TaxYear,
				 LineDesc,
				 LTRIM(RTRIM(Sec4980HTransRelief)) as Sec4980HTransRelief
			  from dbo.PR1094ALEMemberInfo
		) x pivot( max(Sec4980HTransRelief) 
			  for LineDesc in (Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec], [All 12 Months])) Sec4980HTransReliefFlag
	),
	ParsedContactName as
	(   select * from
		(  select h.PRCo, 
				 TaxYear,
				 parsedName.Ordinal,
				 parsedName.StringValue
		   from PR1094Header h
		   cross apply dbo.vfSplit(h.ALEContactName, ' ') as parsedName
		) x pivot( max(StringValue) 
			  for Ordinal in ([1], [2], [3], [4], [5], [6])) ContactName
	)
	Select form1094h.*,
		   left(form1094h.ALEPostalCode, 5) as USZIPCd,
		   Case when len(form1094h.ALEPostalCode) in (9,10) then right(form1094h.ALEPostalCode, 4) Else null End as USZIPExtensionCd,
		   Replace(Replace(form1094h.ALEEIN, ' ',''), '-', '') as formattedEIN,
		   Replace(Replace(Replace(Replace(form1094h.ALEContactPhone, ')', ''), '(', '') , ' ',''), '-', '') as formattedPhoneNum,
		   LTRIM(RTRIM(isnull(pcn.[1], ''))) as ALEContactFirstName,
		   LTRIM(RTRIM  (isnull(pcn.[2], ' ') + ' ' + isnull(pcn.[3], ' ') + ' ' + isnull(pcn.[4], ' '))  ) as ALEContactLastName,
		   IsNull((select count(*) from dbo.PR1095Header h where h.PRCo = @PRCo and h.TaxYear = @TaxYear), 0) as Total1095CEmpCount,

		   mec.Jan  as Jan_MECFlag,
		   mec.Feb  as Feb_MECFlag,
		   mec.Mar  as Mar_MECFlag,
		   mec.Apr  as Apr_MECFlag,
		   mec.May  as May_MECFlag,
		   mec.June as Jun_MECFlag, 
		   mec.July as Jul_MECFlag,
		   mec.Aug  as Aug_MECFlag,
		   mec.Sept as Sep_MECFlag,
		   mec.Oct  as Oct_MECFlag,
		   mec.Nov  as Nov_MECFlag,
		   mec.Dec  as Dec_MECFlag,
		   mec.[All 12 Months] as Yearly_MECFlag,

		   Isnull(ftc.Jan, 0)  as Jan_FullTimeEmplCount,
		   Isnull(ftc.Feb, 0)  as Feb_FullTimeEmplCount,
		   Isnull(ftc.Mar, 0)  as Mar_FullTimeEmplCount,
		   Isnull(ftc.Apr, 0)  as Apr_FullTimeEmplCount,
		   Isnull(ftc.May, 0)  as May_FullTimeEmplCount,
		   Isnull(ftc.June,0)  as Jun_FullTimeEmplCount,
		   Isnull(ftc.July,0)  as Jul_FullTimeEmplCount,
		   Isnull(ftc.Aug, 0)  as Aug_FullTimeEmplCount,
		   Isnull(ftc.Sept,0)  as Sep_FullTimeEmplCount,
		   Isnull(ftc.Oct, 0)  as Oct_FullTimeEmplCount,
		   Isnull(ftc.Nov, 0)  as Nov_FullTimeEmplCount,
		   Isnull(ftc.Dec, 0)  as Dec_FullTimeEmplCount,
		   Isnull(ftc.[All 12 Months], 0) as Yearly_FullTimeEmplCount,

		   Isnull(tec.Jan, 0)  as Jan_TotalEmplCount,
		   Isnull(tec.Feb, 0)  as Feb_TotalEmplCount,
		   Isnull(tec.Mar, 0)  as Mar_TotalEmplCount,
		   Isnull(tec.Apr, 0)  as Apr_TotalEmplCount,
		   Isnull(tec.May, 0)  as May_TotalEmplCount,
		   Isnull(tec.June, 0) as Jun_TotalEmplCount,
		   Isnull(tec.July, 0) as Jul_TotalEmplCount,
		   Isnull(tec.Aug, 0)  as Aug_TotalEmplCount,
		   Isnull(tec.Sept, 0) as Sep_TotalEmplCount,
		   Isnull(tec.Oct, 0)  as Oct_TotalEmplCount,
		   Isnull(tec.Nov, 0)  as Nov_TotalEmplCount,
		   Isnull(tec.Dec, 0)  as Dec_TotalEmplCount,
		   Isnull(tec.[All 12 Months], 0) as Yearly_TotalEmplCount,

		   ag.Jan  as Jan_AggregatedGroupFlag,
		   ag.Feb  as Feb_AggregatedGroupFlag,
		   ag.Mar  as Mar_AggregatedGroupFlag,
		   ag.Apr  as Apr_AggregatedGroupFlag,
		   ag.May  as May_AggregatedGroupFlag,
		   ag.June as Jun_AggregatedGroupFlag,
		   ag.July as Jul_AggregatedGroupFlag,
		   ag.Aug  as Aug_AggregatedGroupFlag,
		   ag.Sept as Sep_AggregatedGroupFlag,
		   ag.Oct  as Oct_AggregatedGroupFlag,
		   ag.Nov  as Nov_AggregatedGroupFlag,
		   ag.Dec  as Dec_AggregatedGroupFlag,
		   ag.[All 12 Months] as Yearly_AggregatedGroupFlag,

		   tr.Jan  as Jan_TransReliefFlag,
		   tr.Feb  as Feb_TransReliefFlag,
		   tr.Mar  as Mar_TransReliefFlag,
		   tr.Apr  as Apr_TransReliefFlag,
		   tr.May  as May_TransReliefFlag,
		   tr.June as Jun_TransReliefFlag,
		   tr.July as Jul_TransReliefFlag,
		   tr.Aug  as Aug_TransReliefFlag,
		   tr.Sept as Sep_TransReliefFlag,
		   tr.Oct  as Oct_TransReliefFlag,
		   tr.Nov  as Nov_TransReliefFlag,
		   tr.Dec  as Dec_TransReliefFlag,
		   tr.[All 12 Months] as Yearly_TransReliefFlag
	from dbo.PR1094Header form1094h
	left join MECOfferIndicators mec
		on form1094h.PRCo     = mec.PRCo
		and form1094h.TaxYear = mec.TaxYear
	left join FullTimeEmployeeCounts ftc
		on ftc.PRCo     = mec.PRCo
		and ftc.TaxYear = mec.TaxYear
	left join TotalEmployeeCounts tec
		on  tec.PRCo    = mec.PRCo
		and tec.TaxYear = mec.TaxYear
	left join AggregatedGroupIndicators ag
		on  ag.PRCo    = mec.PRCo	
		and ag.TaxYear = mec.TaxYear
	left join Sec4980HTransReliefFlags tr
		on  tr.PRCo    = mec.PRCo
		and tr.TaxYear = mec.TaxYear
	left join ParsedContactName pcn
		on pcn.PRCo     = form1094h.PRCo
		and pcn.TaxYear = form1094h.TaxYear
	where mec.TaxYear = @TaxYear
	  and mec.PRCo    = @PRCo
 

	--Populate the corresponding PR1095Covered tracking data.
	;With GeneratedHistoryKeyValues as
	(
			Select  h.PRCo,
			        h.TaxYear,
					h.Employee,
					@TransmissionID as NewTransmissionID,
			  		Row_Number() Over (Partition By h.PRCo, h.TaxYear 
										   Order By h.PRCo, h.TaxYear, h.Employee) as NewRecordID
				from dbo.PR1095Header h
				where h.TypeOVC = @TransmissionType
	), 
	--Normalize/Pivot months to columns in order to get a one to one relationship between PR1095Header (Keyed on:  Company, TaxYear, EmployeeID --> Employee Personal Info)
	--and the associated monthly offer codes in PR1095EmployeeDetail (Keyed on:  Month, Company, TaxYear, EmployeeID --> CoverageOfferCode, EmployeeShareAmount, SafeHarborCode)
	--To make it easier to digest client side, join up with PR1095Header.
	CoverageOffers as
	(  select * from
	   (  select PRCo,
				TaxYear,
				Employee,
				TaxMonth,
				CoverageOfferCode
			  from dbo.PR1095EmployeeDetail
		) x pivot( max(CoverageOfferCode) 
			  for TaxMonth in (Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec])) PivotOffers
	),
	EmployeShareAmounts as
	(  select * from
	   (  select PRCo,
				TaxYear,
				Employee,
				TaxMonth,
				EmployeeShareAmt
			  from dbo.PR1095EmployeeDetail
		) x pivot ( max(EmployeeShareAmt) 
			  for TaxMonth in (Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec])) PivotEmployeeShareAmount
	),   
	SafeHarborCodes as
	(  select * from
	   (  select PRCo,
				TaxYear,
				Employee,
				TaxMonth,
				Sec4980HCode
			  from dbo.PR1095EmployeeDetail
		) src pivot( max(Sec4980HCode) 
				for TaxMonth in (Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec])) PivotSafeHarborCodes
	)
	Select form1095h.*,
		   GeneratedHistoryKeyValues.NewTransmissionID,
		   GeneratedHistoryKeyValues.NewRecordID,
		   left(form1095h.Zip, 5) as USZipCd,
		   Case when len(form1095h.Zip) in (9,10) then right(form1095h.Zip, 4) Else null End as USZipExtensionCd,
		   Replace(Replace(form1095h.SSN, ' ',''), '-', '') as formattedSSN, 
		   form1095h.PlanStartMonth, 
		   co.Jan   as Jan_CoverageOffer,
		   co.Feb   as Feb_CoverageOffer,
		   co.Mar   as Mar_CoverageOffer,
		   co.Apr   as Apr_CoverageOffer,
		   co.May   as May_CoverageOffer,
		   co.June  as Jun_CoverageOffer,
		   co.July  as Jul_CoverageOffer,
		   co.Aug   as Aug_CoverageOffer,
		   co.Sept  as Sep_CoverageOffer,
		   co.Oct   as Oct_CoverageOffer,
		   co.Nov   as Nov_CoverageOffer,
		   co.[Dec] as Dec_CoverageOffer,
		   sh.Jan   as Jan_SafeHarbor,
		   sh.Feb   as Feb_SafeHarbor,
		   sh.Mar   as Mar_SafeHarbor,
		   sh.Apr   as Apr_SafeHarbor,
		   sh.May   as May_SafeHarbor,
		   sh.June  as Jun_SafeHarbor,
		   sh.July  as Jul_SafeHarbor,
		   sh.Aug   as Aug_SafeHarbor,
		   sh.Sept  as Sep_SafeHarbor,
		   sh.Oct   as Oct_SafeHarbor,
		   sh.Nov   as Nov_SafeHarbor,
		   sh.Dec   as Dec_SafeHarbor,
		   es.Jan   as Jan_ShareAmount,
		   es.Feb   as Feb_ShareAmount,
		   es.Mar   as Mar_ShareAmount,
		   es.Apr   as Apr_ShareAmount,
		   es.May   as May_ShareAmount,
		   es.June  as Jun_ShareAmount,
		   es.July  as Jul_ShareAmount,
		   es.Aug   as Aug_ShareAmount,
		   es.Sept  as Sep_ShareAmount,
		   es.Oct   as Oct_ShareAmount,
		   es.Nov   as Nov_ShareAmount,
		   es.[Dec] as Dec_ShareAmount
	from dbo.PR1095Header form1095h
	inner join CoverageOffers co  
	    on form1095h.PRCo = co.PRCo
		and form1095h.TaxYear  = co.TaxYear
		and form1095h.Employee = co.Employee
	inner join EmployeShareAmounts es
		on  co.PRCo     = es.PRCo
		and co.TaxYear  = es.TaxYear
		and co.Employee = es.Employee
	inner join SafeHarborCodes sh
		on  sh.PRCo     = es.PRCo
		and sh.TaxYear  = es.TaxYear
		and sh.Employee = es.Employee
	inner join GeneratedHistoryKeyValues
		on  sh.Employee = GeneratedHistoryKeyValues.Employee
		and sh.TaxYear  = GeneratedHistoryKeyValues.TaxYear
		and sh.PRCo     = GeneratedHistoryKeyValues.PRCo
	where 
	  	  co.TaxYear        = @TaxYear
	  and co.PRCo           = @PRCo
	  and form1095h.TypeOVC = @TransmissionType
	                                                  --For original transmissions the transmissionID must be null.
	  and IsNull(form1095h.TransmissionID, -1) = Case When @TransmissionType in ('O') and @IsReplacement = 0 
	                                                    Then -1 
													  --For replacement transmissions that failed due to business rules the user must set the transmission
	                                                  --status to rejected, enter the receiptID, choose replacement transmission and re-export.
												      When @TransmissionType in ('O') and @IsReplacement = 1
	                                                    Then IsNull(form1095h.TransmissionID, 0) 
													   --For corrected transmissions the transmissionID must be non null.
													  When @TransmissionType in ('C') 
													    Then IsNull(form1095h.TransmissionID, 0) 
												 End
	order by 
		form1095h.PRCo,
	    form1095h.TaxYear,
		form1095h.Employee


	Select form1095c.*,
		   LTRIM(RTRIM(Replace(Replace(form1095c.SSN, ' ',''), '-', ''))) as formattedSSN,
		   LTRIM(RTRIM(Cast(Convert(Date, LTRIM(RTRIM(form1095c.DOB)), 105) as char(10)))) as formattedDOB
		from dbo.PR1095Covered form1095c
		where 
			form1095c.TaxYear = @TaxYear
			and form1095c.PRCo = @PRCo
		 and exists(select 1 from dbo.PR1095Header h 
							  where h.Employee = form1095c.Employee
								and h.TaxYear  = form1095c.TaxYear
								and h.PRCo     = form1095c.PRCo
								and h.TypeOVC  = @TransmissionType)
		order by 
			form1095c.Employee,
		    form1095c.Seq


	select pr1094ALEOM.*, 
		   LTRIM(RTRIM(Replace(Replace(pr1094ALEOM.EIN, ' ',''), '-', ''))) as formattedEIN,
		   (select count(*) + 1 from dbo.PR1094ALEOtherMembers om where om.TaxYear = @TaxYear and om.PRCo = @PRCo) as TotalForm1095CALEMemberCnt
		from dbo.PR1094ALEOtherMembers pr1094ALEOM
		where @TaxYear = TaxYear
		  and @PRCo = PRCo


	select hist.*
		from dbo.PRACAeFileTransmissionHistory hist
		where hist.TransmissionID = @PreviousTransmissionID
		  and hist.TaxYear = @TaxYear
		  and hist.PRCo = @PRCo

	select hist.*
		from dbo.PRACAeFileTransmissionHistory hist
		where hist.TaxYear = @TaxYear
		  and hist.PRCo = @PRCo

End Try

Begin Catch
    Declare @ErrorSeverity INT = ERROR_SEVERITY(),
            @ErrorState    INT = ERROR_STATE();
    RaisError (@ErrorMessage, @ErrorSeverity, @ErrorState);
End Catch
Go

