IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[1095Initilization]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[1095Initilization]
GO


SET QUOTED_IDENTIFIER ON
GO

/************************************************************
* CREATED BY:    Craig Johnson	06/22/15	
*					
*
* USAGE:   Called from frmPRACAInitilize.  This sproc populates the PR1095Header, PR1095EmployeeDetail and PR1095Covered tables. 
  		   Processing is as follows:
		   		    	
	1)  Clear PR1095Header and PR1095EmployeeDetail for current company and tax year if user chooses to overwrite existing data.
	2)  If user does not choose to overwrite existing data capture existing key values.  This table is used to exclude pre-existing
		employees from updates (ie, only newly inserted employees will be updated).
	3)  Build PR1095Header records.
	4)  Build PR1095Detail records from PR1095Header.
	5)  Update fields corresponding with parameters passed through from the PR ACA Initialize form.
	6)  If a user chooses to initialize from HR, populate PR1095Covered (covered individuals) as a stub, leaving each months offer flag 
		set to 'N' if the employer offers a self insured plan.
	7)  If a user chooses to also build from HR History:
			1)  Utilize the ACA history tables to update the offers of coverage to the employees for each month in PR1095EmployeeDetail.
			2)  Utilize actual coverage to update PR1095EmployeeDetail.  If a plan meets MEC and an employee was covered
			    a 1A offer will overwrite whatever data was populated in 1) above.
			3)  Utilize actual coverage to update the montly coverage flags in PR1095Covered.
	8)  Toggle the yearly flags in PR1095Covered and PR1095Header/PR1095EmployeeDetail to conform with instructions provided on the form
	    by the IRS.


* 
* INPUT PARAMETERS
*   @PRCo				PR Company
*   @TaxYear			Tax year to initialize.
*   @OverwriteExistingData If 'N', do not update any associated tables/data for employees that already exist in PR1095Header.
*   @InitEmployee		Initialize only full time or all ('FT' or 'ALL')
*	@FullTimeThreshold  Use 'W'eekly calculation or 'M'onthly for FTE classification
*	@PROfferCode        Passed through from form to PR1095Header/PR1095EmployeeDetail
*	@PRSelfCost			Passed through from form to PR1095Header/PR1095EmployeeDetail
*	@PRSafeHarbor       Passed through from form to PR1095Header/PR1095EmployeeDetail
*   @UseACAHistory      If 'Y' flags for offers of coverage will be updated in PR1095Header/PR1095EmployeeDetail and PR1095Covered.
*
*
* OUTPUT PARAMETERS
*   @InitializedCount  Number of employees that are that have been added and updated.
*
* RETURN VAL
*   Not used.  If this sproc fails it is non-recoverable. The
*   transaction will be rolled back and the message will be caught
*   in the Try/Catch client side and displayed to the user.
************************************************************/
CREATE PROCEDURE [dbo].[1095Initilization]
 @PRCo   bCompany,
 @HRCo bCompany,
 @TaxYear char(4), 
 @OverwriteExistingData char(1),
 @InitEmployee char(3),
 @InitFrom char(2),
 @PROfferCode char(2),
 @PRSelfCost char(15),
 @PRSafeHarbor char(2),
 @PlanStartMonth char(2),
 @IncludeDependents char(1),
 @InitializedCount int Output	
As
BEGIN
	SET NOCOUNT ON

	Set @InitializedCount = 0;

	Declare @InsertedCount int = 0;
	Declare @DeletedCount int = 0;
	DECLARE	@StartOfTaxYear	SMALLDATETIME;
	DECLARE @EndOfTaxYear		SMALLDATETIME;

	SET	@StartOfTaxYear = dbo.vfDateCreate(1, 1, @TaxYear);
	SET @EndOfTaxYear    = dbo.vfDateCreate(12, 31, @TaxYear);

	Begin Transaction

	--Will need to cross join to this downstream to populate one month for each employee in 1095EmployeeDetail.
	if Object_ID('tempdb..#Months') is not null
		drop table #Months;
	
	Select * into #Months from
	(
		Select 1 as [Number], 'Jan' as Name
		Union All
		Select 2, 'Feb'
		Union All
		Select 3, 'Mar'
		Union All
		Select 4, 'Apr'
		Union All
		Select 5, 'May'
		Union All
		Select 6, 'June'
		Union All
		Select 7, 'July'
		Union All
		Select 8, 'Aug'
		Union All
		Select 9, 'Sept'
		Union All
		Select 10, 'Oct'
		Union All
		Select 11, 'Nov'
		Union All
		Select 12, 'Dec'
	) as Months

	if Object_ID('tempdb..#ExcludedDependentRecords') is not null
		drop table #ExcludedDependentRecords;

	Create Table #ExcludedDependentRecords
	(
		PRCo tinyint,
		Employee int,
		TaxYear char(4),
		DependentID int
	)

	if Object_ID('tempdb..#IncludedEmployees') is not null
		drop table #IncludedEmployees;

	--Pull in data needed for 1095 form from PREH for each employee.  This table will contain a record for each month of the tax year
	--for each employee as well as a flag indicating the full time ACA status for the given month.  The PRACAMonthlyHoursSummary view does most 
	--the work, and can be queried directly for reference.
	Select
		   mh.PRCo,
		   mh.Employee,
		   mh.HRCo,
		   mh.HRRef,
		   @TaxYear as TaxYear,
		   mh.[Month],
		   Case when mh.IsFullTimeMonthly = 'Y' or mh.IsFullTimeWeekly  = 'Y' Then 'Y'
			 Else 'N'
		   End as IsFullTime,
		   mh.FirstName,
		   mh.LastName,
		   mh.SSN,
		   mh.BirthDate,
		   mh.[Address],
		   mh.City,
		   mh.State,
		   mh.Zip,
		   mh.Country
		into #IncludedEmployees
		from dbo.PRACAMonthlyHoursSummary mh
		where mh.[Year] = @TaxYear
		  and mh.PRCo = @PRCo

		  
	--If the 'Initialize from HR' checkbox is checked, remove any records not in the HRCo specified by the user.	  
	If(@InitFrom = 'HR')
		Delete ie from #IncludedEmployees ie where ISNULL(ie.HRCo,0) <> ISNULL(@HRCo,0);
	


	If (@OverwriteExistingData = 'N')
	BEGIN
		--Delete existing records out of the summary table to prevent downstream updates if the user chooses not to overwrite.
		Delete ie from #IncludedEmployees ie 
		          where exists(select 1 from dbo.PR1095Header h 
				                        where h.PRCo = ie.PRCo 
										  and h.Employee = ie.Employee
										  and h.TaxYear = ie.TaxYear)

		--When choosing not to overwrite, these dependents will not be updated.  										
		Insert into #ExcludedDependentRecords
			Select c.PRCo, 
				   c.Employee, 
				   c.TaxYear,
				   c.DependentID 
				from dbo.PR1095Covered c
				where c.PRCo = @PRCo
				  and c.TaxYear = @TaxYear
				  and c.DependentID is not null
		

	END
	ELSE --Clear the 1095 tables and do a complete refresh.
	BEGIN

		delete c from dbo.PR1095Covered c
			     where c.PRCo = @PRCo
				 and c.TaxYear = @TaxYear

		Delete d from dbo.PR1095EmployeeDetail d 
				 where d.PRCo = @PRCo 
				 and d.TaxYear = @TaxYear

		Delete h from dbo.PR1095Header h 
				 where h.PRCo = @PRCo 
				 and h.TaxYear = @TaxYear
	END	



		--Insert employees into the header table.  If the user chose not to overwrite existing data, existing records were removed from the 
		--summary table and will not be part of the insert.  If they are overwriting the 1095 tables are empty and everything for the current
		--tax year and company will be inserted.
		;With AddedEmployeeRecords as
		(
				Select 
					   ie.PRCo as PRCo,
					   ie.TaxYear as TaxYear,
					   ie.Employee as Employee,
					   min(ie.LastName) as LastName,
					   min(ie.FirstName) as FirstName,
					   min(ie.SSN) as SSN,
					   min(ie.[Address]) as Address,
					   min(ie.City) as City,
					   min(ie.[State]) as [State],
					   min(ie.Zip) as Zip,
					   min(ie.Country) as Country,
					   @PlanStartMonth as PlanStartMonth
					from #IncludedEmployees ie
					group by 
						ie.PRCo,
						ie.TaxYear,
						ie.Employee  
		)
		Insert Into dbo.PR1095Header ( [PRCo]
									  ,[TaxYear]
				                      ,[Employee]
				                      ,[LastName]
				                      ,[FirstName]
				                      ,[SSN]
				                      ,[Address]
				                      ,[City]
				                      ,[State]
				                      ,[Zip]
				                      ,[Country]
									  ,[PlanStartMonth])	
		Select  e.PRCo
			   ,e.TaxYear
			   ,e.Employee
		       ,e.LastName
			   ,e.FirstName
			   ,e.SSN
			   ,e.Address
			   ,e.City
			   ,e.State
			   ,e.Zip
			   ,e.Country
			   ,e.PlanStartMonth
		from AddedEmployeeRecords e

		Set @InsertedCount = @@ROWCOUNT


		--If any month was flagged as full time from either the weekly criteria or monthly criteria, flag as full time for the year.
		Update h set h.IsFullTime = 'Y' 
					from dbo.PR1095Header h
					--If we find a a 'Y' flag for any month set it in the header
					where exists (select 1 from #IncludedEmployees ie 
										   where ie.PRCo = h.PRCo 
											 and ie.Employee = h.Employee 
											 and ie.TaxYear = h.TaxYear 
											 and ie.IsFullTime = 'Y')

		--Possible values are Full Time ('FT') and 'ALL'.  
		If(@InitEmployee = 'FT')
		BEGIN

			--Delete from dependent tables before deleting records from the parent PR1095Header
			Delete d from dbo.vPR1095EmployeeDetail d
			join dbo.PR1095Header h 
			on h.PRCo = d.PRCo
			and h.TaxYear = d.TaxYear
			and h.Employee = d.Employee
			where h.IsFullTime = 'N'
			and exists (select 1 from #IncludedEmployees ie 
								    	where ie.PRCo = h.PRCo 
										  and ie.Employee = h.Employee 
										  and ie.TaxYear = h.TaxYear)	

			Delete c from dbo.vPR1095Covered c
			join dbo.PR1095Header h 
			on h.PRCo = c.PRCo
			and h.TaxYear = c.TaxYear
			and h.Employee = c.Employee
			where h.IsFullTime = 'N'
			and exists (select 1 from #IncludedEmployees ie 
								    	where ie.PRCo = h.PRCo 
										  and ie.Employee = h.Employee 
										  and ie.TaxYear = h.TaxYear)	

			--Delete records that did not meet full time criteria for at least one month if they were not removed from the summary table.
			Delete h from dbo.PR1095Header h 
			         where h.IsFullTime = 'N'
					 --Employees removed from the summary table will not be updated.
					 and exists (select 1 from #IncludedEmployees ie 
									    	where ie.PRCo = h.PRCo 
											  and ie.Employee = h.Employee 
											  and ie.TaxYear = h.TaxYear)	
											  
			Set @DeletedCount = @@ROWCOUNT		
		END
					 	
		--Number of Employees written/updated will be returned to the UI.			 	
		Set @InitializedCount = @InsertedCount - @DeletedCount

		--Pass the employee detail parameters from the form through to the detail record.
		--If no other history is found for a given month, the header record will be
		--flipped downstream.
		Update d set d.CoverageOfferCode = IsNull(@PROfferCode, ''),
					 d.EmployeeShareAmt  = Cast(@PRSelfCost as numeric(12,2)),
					 d.Sec4980HCode      = IsNull(@PRSafeHarbor, '')
				 from dbo.PR1095EmployeeDetail d
				 --Employees removed from the summary table will not be updated.
				 where exists (select 1 from #IncludedEmployees ie 
									    where ie.PRCo = d.PRCo 
									      and ie.Employee = d.Employee 
										  and ie.TaxYear = d.TaxYear)


		  /*
			Two temp tables are created when useres choose to Initialize from HR to store employee and dependent coverage history
			we have the same information in two different places.  The data is then used to populate PR1095Covered when an employer
			has a self insured plan --

				First:  #EmployeesAndDependentCoverageFromACAHist - populated from vHRResBenACACoveragehist.  This was to allow import of
				                                                    external data if customers are tracking outside of our HR Module.

				Second: #EmployeesAndDependentCoverageFromHREB - populated from HREB (Form:HR Resource Benefits/Tab:Info), which is used 
				                                                 to track actual coverage in the HR Module.  If they use the HR module properly the same
															     information as above will exist here.

			    Coverage history on the actual employee can also be imported into vHRResourceMasterACAHistory (Form: HR Resource Master/Tab: ACA History).
				This is handled seperately only when the user chooses to 'Use HR History'.  If using HR History this data will be utilized to populate 
				the monthly offer of coverage fields in PR1095EmployeeDetail with Series 1/2 codes.
				
				If the user chooses to 'Initialize from HR' and not to use HR history, only the PR1095Covered table will be populated.  
		  */
		  If(@InitFrom = 'HR')
		  Begin
				--Populate Employee and Dependent coverage history from vHRResBenACACoverageHist (HR Resource Benifits/ACA Coverage Hist).
				if object_id('tempdb..#EmployeeAndDependentCoverage') is not null
					drop table #EmployeeAndDependentCoverage;

				Create table #EmployeeAndDependentCoverage
				(
					HRCo tinyint,
					PRCo tinyint,
					HRRef int,
					DependentID int,
					Employee int,
					BenefitCode varchar(10),
					ACAHealthCareYN char(1),
					ACAMinEssenCoverage char(1),
					ACASelfInsured char(1),
					EffectDate datetime,
					EndDate datetime
				)

				--Grab the keys for all possible dependents.
				--HREB and HRDP may have incomplete dependent sequences.
				--Grab all possible keys, and let the implicit group by 
				--take care of any duplication.
				;With EmployeesAndDependents as
				(
					Select  eb.HRCo as HRCo,
							eb.HRRef as HRRef,
							eb.DependentSeq as [Seq]
						From dbo.HREB eb
					Union
					Select dp.HRCo,
						   dp.HRRef,
						   dp.Seq
						From dbo.HRDP dp
				)
				Insert into #EmployeeAndDependentCoverage
				(
					HRCo,
					PRCo,
					HRRef,
					DependentID,
					Employee,
					BenefitCode,
					ACAHealthCareYN,
					ACAMinEssenCoverage,
					ACASelfInsured,
					EffectDate,
					EndDate
				)
				(
					Select  distinct rm.HRCo,
							rm.PRCo, 
							rm.HRRef,
							ed.Seq,
							rm.PREmp,
							bc.BenefitCode,
							bc.ACAHealthCareYN,
							bc.ACAMinEssenCoverageYN,
							bc.ACASelfInsuredYN,
							eb.EffectiveDate,
							eb.[ExpireDate]
					from EmployeesAndDependents ed
					inner join dbo.HRRM rm
						on rm.HRCo = ed.HRCo 
						and rm.HRRef = ed.HRRef
					left join dbo.HRResBenACACoverageHist eb
						on eb.HRCo = rm.HRCo
						and eb.HRRef = rm.HRRef
						and eb.DependentSeq = ed.Seq
					left join dbo.HRBC bc
						on bc.HRCo = rm.HRCo
						and bc.BenefitCode = eb.BenefitCode
					where not exists (select 1 from #ExcludedDependentRecords ecr 
											   where ecr.Employee = rm.PREmp 
												 and ecr.PRCo     = rm.PRCo 
												 and ecr.DependentID = eb.DependentSeq)
					  and  exists (select 1 from dbo.PR1095Header h 
											where h.PRCo =  @PRCo
											and h.Employee = rm.PREmp 
											and h.TaxYear = @TaxYear)
					and datepart(year, eb.EffectiveDate) = @TaxYear
					and rm.PRCo = @PRCo

					UNION

					Select  rm.HRCo,
							rm.PRCo, 
							rm.HRRef,
							ed.Seq,
							rm.PREmp,
							bc.BenefitCode,
							bc.ACAHealthCareYN,
							bc.ACAMinEssenCoverageYN,
							bc.ACASelfInsuredYN,
							eb.EffectDate,
							eb.EndDate
					from EmployeesAndDependents ed
					inner join dbo.HRRM rm
						on rm.HRCo = ed.HRCo 
						and rm.HRRef = ed.HRRef
					left join dbo.HREB eb
						on eb.HRCo = rm.HRCo
						and eb.HRRef = rm.HRRef
						and eb.DependentSeq = ed.Seq
					left join dbo.HRBC bc
						on bc.BenefitCode = eb.BenefitCode
						and bc.HRCo = rm.HRCo
					where not exists (select 1 from #ExcludedDependentRecords ecr 
										       where ecr.Employee = rm.PREmp 
											     and ecr.PRCo     = rm.PRCo 
												 and ecr.DependentID = eb.DependentSeq)
					  and  exists (select 1 from dbo.PR1095Header h
						                    where h.PRCo =  @PRCo
							                and h.Employee = rm.PREmp 
											and h.TaxYear = @TaxYear)
					  and Datepart(year,eb.EffectDate) = @TaxYear
					  and eb.ActiveYN = 'Y'
					  and rm.PRCo = @PRCo
				)

				Update h set h.SelfInsCoverage = 'Y'
					from dbo.PR1095Header h
					inner join #EmployeeAndDependentCoverage edc
						on h.Employee = edc.Employee
					    and h.PRCo = edc.PRCo
					Where Datepart(year,edc.EffectDate) = @TaxYear		
						and edc.ACASelfInsured = 'Y'
					--Employees removed from the summary table will not be updated.
					and exists (select 1 from #IncludedEmployees ie 
					                     where ie.PRCo = h.PRCo 
										   and ie.Employee = h.Employee 
										   and ie.TaxYear = h.TaxYear)
					and edc.DependentID = 0 




				If @IncludeDependents = 'Y'
				BEGIN

					if object_id('tempdb..#PR1095Covered') is not null
						drop table #PR1095Covered;

					--Write out the key values that will be written to PR1095Covered.  Once all the associated fields are populated 
					--the records will get inserted into the table. 
					Select distinct
					    @TaxYear as TaxYear,
						eb.HRCo as HRCo,
						eb.PRCo as PRCo,
						eb.HRRef as HRRef,
						null as Seq,						
						eb.DependentID as DependentID,
						eb.Employee as Employee,
						cast(null as varchar(30)) as FirstName,
						cast(null as varchar(30)) as LastName,
						cast(null as varchar(11)) as SSN,
						cast(null as smalldatetime) as DOB,
						cast(null as char(2)) as CoveredAll12Months,
						cast(null as char(1)) as Jan,
						cast(null as char(1)) as Feb,
						cast(null as char(1)) as Mar,
						cast(null as char(1)) as Apr,
						cast(null as char(1)) as May,
						cast(null as char(1)) as June,
						cast(null as char(1)) as July,
						cast(null as char(1)) as Aug,
						cast(null as char(1)) as Sept,
						cast(null as char(1)) as Oct,
						cast(null as char(1)) as Nov,
						cast(null as char(1)) as [Dec]
					into #PR1095Covered 
					from #EmployeeAndDependentCoverage eb
					where   IsNull(eb.ACAHealthCareYN,'N') = 'Y'
						and IsNull(eb.ACASelfInsured, 'N') = 'Y'
						and not exists (select 1 from dbo.PR1095Covered c 
											    where eb.Employee = c.Employee
												    and eb.DependentID = c.DependentID
													and eb.PRCo = c.PRCo
													and c.TaxYear = @TaxYear)
					and eb.PRCo  = @PRCo
							 
					--MaxSequenceByEmployee: Gets the max sequence
					--SequenceNumbers: Sequences by c.TaxYear, c.PRCo, c.Employee starting from the max sequence
					--Update the sequence in PR1095Covered
					;With MaxSequenceByEmployee as
					(
						Select c.TaxYear,
							   c.PRCo,
						       c.Employee,
						       max(c.Seq) as SequenceOffset
						from dbo.PR1095Covered c 
						where c.TaxYear = @TaxYear 
							and c.PRCo = @PRCo
						Group By
							c.TaxYear,
							c.PRCo,
							c.Employee
					),
					SequenceNumbers as
					(
						Select c.TaxYear, 
					      	   c.PRCo,
							   c.Employee,
							   c.DependentID,
							   Row_Number() over(Partition By c.TaxYear, c.PRCo, c.Employee order by c.TaxYear, c.PRCo, c.Employee, c.DependentID) + IsNull(ms.SequenceOffset, 0) as [Sequence]
							from #PR1095Covered c
							left join MaxSequenceByEmployee ms
								on ms.PRCo = c.PRCo
								and ms.Employee = c.Employee
								and ms.TaxYear = c.TaxYear
					)
					Update c set c.Seq = SequenceNumbers.[Sequence] 
							 from #PR1095Covered c
							 inner join SequenceNumbers SequenceNumbers
							 	 on c.PRCo = SequenceNumbers.PRCo
								 and c.Employee = SequenceNumbers.Employee
								 and c.DependentID = SequenceNumbers.DependentID
								 and c.TaxYear = SequenceNumbers.TaxYear
		 

					 --Update PI from HR Resource Master for the Employee.
					Update c set c.FirstName = rm.FirstName, 
								 c.LastName = rm.LastName,
								 c.SSN = rm.SSN,
								 c.DOB = rm.BirthDate,
								 c.Employee = rm.PREmp
							from #PR1095Covered c
							inner join dbo.HRRM rm
								on rm.HRCo = c.HRCo
								and rm.HRRef = c.HRRef
							where   rm.PRCo = @PRCo
								and c.DependentID = 0
							
					--Update PI for dependents from HR Resource Dependents.		
					Update c Set c.FirstName = dp.FirstName,
								 c.LastName  = dp.Name,
								 --null out the DOB of the dependents when we have an SSN on file.  This is an explicit IRS instruction on the 1095C-3 tax form.					
								 c.DOB = Case When dp.SSN is not null then null else dp.BirthDate End,
								 c.SSN = dp.SSN									  
					from #PR1095Covered c
					inner join dbo.HRDP dp
						ON dp.HRCo = c.HRCo
						and dp.HRRef = c.HRRef
						and dp.Seq = c.DependentID 


					--null out the DOB of the primary when we have an SSN on file.  This is an explicit IRS instruction on the 1095C-3 tax form.
					Update c set  c.DOB = Case When c.SSN is not null then null else c.DOB End
							from #PR1095Covered c
							where c.DependentID = 0
					
                    --Write out any missing dependents when the user chooses to IncludeDependents.
					Insert into dbo.PR1095Covered (PRCo, TaxYear, Employee, Seq,  DependentID, LastName, FirstName, SSN, DOB, CoveredAll12Months, 
												   Jan, Feb, Mar, Apr, May, June, July, Aug, Sept, Oct, Nov, [Dec])
						Select ct.PRCo,
							   ct.TaxYear as TaxYear,
							   ct.Employee as Employee,
							   ct.Seq as Seq,
							   ct.DependentID as DependentID,
							   ct.LastName as LastName,
							   ct.FirstName as FirstName,
							   ct.SSN as SSN,
							   ct.DOB as DOB,
							   'N' as CoveredAll12Months,
							   'N' as Jan,
							   'N' as Feb,
							   'N' as Mar,
							   'N' as Apr,
							   'N' as May,
							   'N' as June,
							   'N' as July,
							   'N' as Aug,
							   'N' as Sept,
							   'N' as Oct,
							   'N' as Nov,
							   'N' as [Dec]
						from #PR1095Covered ct
					    where not exists (select 1 from dbo.PR1095Covered c 
											       where ct.Employee = c.Employee
												      and ct.DependentID = c.DependentID
													  and ct.PRCo = c.PRCo
													  and ct.TaxYear = c.TaxYear)
						and ct.PRCo  = @PRCo
						and ct.TaxYear = @TaxYear

					--Resequence dependents ordered by dependentID.  This will always put the employee at the top (Seq = 1)
					--since the employee always has a dependentID of 0.	
					;With SequenceNumbers as
					(
						Select c.TaxYear, 
					      	   c.PRCo,
							   c.Employee,
							   c.KeyID,
							   Row_Number() over(Partition By c.TaxYear, c.PRCo, c.Employee order by c.TaxYear, c.PRCo, c.Employee, IsNull(c.DependentID, 99999)) as [Sequence]
							from dbo.PR1095Covered c
							where c.TaxYear = @TaxYear
							  and c.PRCo = @PRCo
					)
					Update c set c.Seq = SequenceNumbers.[Sequence] 
							 from dbo.PR1095Covered c
							 inner join SequenceNumbers 
							 	 on c.KeyID = SequenceNumbers.KeyID				    

				END --If IncludeDependents = 'Y'	
											

				/*
					When the user chooses to Build from HR history, the flags indicating when coverage was offered and at what times will be updated
						in PR1095EmployeeDetail (Monthly offer/acceptance:  Series 1/2 codes), PR1095Covered (Monthly/Yearly indicating
						covered dependents).  PR1095Header (YearlyOffer) will be updated based on the information written to the PR1095EmployeeDetail table.

							1) PR1095EmployeeDetail.OfferCoverageCode (Series 1) and .Sec4980HCode (Series 2) is updated from vResourceMasterACAHistory.
							2) The values for Series 1/2 codes will be overwritten by the ACTUAL coverage indicated in the system if it exists.
							   This utilizes the #EmployeesAndDependentCoverageFromHREB and #EmployeesAndDependentCoverageFromACAHistore tables built above.
							   This allows for missing history as it is likely this data will not exist the first year the process is ran.  The fact that
							   that the employee had actual coverage that meets MEC takes precedence over having a history record.
							3) PR1095Covered monthly flags are then also updated from #EmployeesAndDependentCoverageFromACAHist and EmployeesAndDependentCoverageFromHREB.

					Update each coverage interval from lowest to highest coverage offered.  Months where greater coverage
					was offered will overwrite months that have lesser coverage with the appropriat offer code.  

					These values can be overwritten downstream with a '1A' offer of coverage if it is found that the 
					Employee had ACTUAL coverage that meets MEC assigned in  Form:HR Resource Benefits/Tab:Info
				*/
				;With SeriesOneCodeRanking AS
				(
				--Ranked from most coverage offered to least.
					Select '1A' SeriesOneCode, 1 Ranking
					Union
					Select '1B', 2
					Union 
					Select '1C', 3
					Union 
					Select '1D', 4
					Union 
					Select '1E', 5
					Union
					Select '1F', 6
					Union
					Select '1G', 7
					Union
					Select '1H', 8
					Union
					Select '1I', 9
				), ACAHistoryDetail AS
				(
						select rmh.HRCo, 
							   rmh.HRRef, 
							   rmh.SeriesOneCode,
							   datepart(month, ActivityDate) as BeginningMonthNumber,
							   datepart(month,isnull(rmh.ExpirationDate, @EndOfTaxYear)) as EndingMonthNumber,
							   datepart(Year,isnull(rmh.ExpirationDate, @EndOfTaxYear)) as EndingYearNumber,
							   Row_Number() OVER (PARTITION BY rmh.HRCo, rmh. HRRef, datepart(month, ActivityDate) ORDER BY bfr.Ranking Asc) AS OfferRank
							from dbo.HRResourceMasterACAHistory rmh
							inner join SeriesOneCodeRanking bfr
								on rmh.SeriesOneCode = bfr.SeriesOneCode
							--Process is entirely based in the context of the current tax year.
							where rmh.SeriesOneCode is not null
							and datepart(year, ActivityDate) = @TaxYear
							group by
							   rmh.HRCo, 
							   rmh.HRRef, 
							   rmh.SeriesOneCode,
							   rmh.ActivityDate,
							   rmh.ExpirationDate,
							   bfr.Ranking 
				),
				CoverageOffers AS
				(
					--Crate a table with coverage offeres from the ACA history tables, and the month range intervals for which they were offered.
					--precedence is ranked according to the highest amount of coverager an employer offered in a 
					--given month and any other offer in the same month is elimnated.
					
					--There could still be overlapping intervals with different ranges of coverage if the offer was entered for
					--different starting months.  Update accordingly, since any coverage offer in this table has a ranking of one regardless of 
					--coverage offer due to the filter (Rank = 1) applied above.  
					--
					--Update rank according to the actual offer code.  Updates will be applied to dbo.PR1095EmployeeDetail from
					--lowest (7) to highest rank (1) such that more inclusive offers of coverage will overwrite lesser offers of coverage. 
					Select h.PRCo,
						   h.Employee,
						   x.SeriesOneCode,
						   x.BeginningMonthNumber,
						   x.EndingMonthNumber,
						   x.EndingYearNumber,
						   r.Ranking OfferRank,
						   cast('N' as char(1)) as IsCoveredForTaxYear
					--partition series one intervals by month
					from ACAHistoryDetail x
					inner join dbo.HRRM rm
						on rm.HRCo = x.HRCo 
						and rm.HRRef = x.HRRef
					inner join dbo.PR1095Header h
						on  rm.PRCo = h.PRCo
						and rm.PREmp = h.Employee
					inner join SeriesOneCodeRanking r
						on r.SeriesOneCode = x.SeriesOneCode
					--If there are multiple offer codes for a given month, the one highest on the list in the ranking table will be assigned
					--a rank of 1.  This will eliminate lower offers of coverage that begin in the same month.  The potential for overlapping
					--intervals is taken care of by the order of the updates to PR1095EmployeeDetail.
					where x.OfferRank = 1
					  and h.PRCo = @PRCo
					  and h.TaxYear = @TaxYear
					  and exists (select 1 from #IncludedEmployees ie 
							               where ie.PRCo = h.PRCo 
							                 and ie.Employee = h.Employee 
								             and ie.TaxYear = h.TaxYear)
				)
				Update d set d.CoverageOfferCode = o.SeriesOneCode
						 from dbo.PR1095EmployeeDetail d   
						 inner join CoverageOffers o
							on o.Employee = d.Employee
							and d.PRCo = o.PRCo
						 inner join #Months m 
							on m.Name = d.TaxMonth
						 where d.PRCo = @PRCo 
							and	d.TaxYear = @TaxYear
							and m.Number between o.BeginningMonthNumber and o.EndingMonthNumber
							and o.OfferRank <= (Select max(Ranking) from SeriesOneCodeRanking)
				            and exists (select 1 from #IncludedEmployees ie 
						                         where ie.PRCo = d.PRCo 
						                           and ie.Employee = d.Employee 
							                       and ie.TaxYear = d.TaxYear)


				

				/*
					Update each acceptance interval from least to most favorable to the employer.  Months where more favorable
					acceptance codes exist will overwrite months that have less favorable acceptabce codes.  
				*/
				--These codes can either indicate the employee enrolled in coverage, did not qualify, or the company is 
				--claiming some sort of safe harbor/reason coverage was not offered.
				;With SeriesTwoCodeRanking AS
				(
					Select '2C' SeriesTwoCode, 1 Ranking --Employee Enrolled in coverage
					Union
					Select '2B', 2 --Employee not a full time employee
					Union 
					Select '2A', 3 --Employee not employed in the month
					Union 
					Select '2D', 4 --The rest of these are safe harbor/reason for non coverage offer and ar ranked arbitrarily.
					Union 
					Select '2E', 5
					Union
					Select '2F', 6
					Union
					Select '2G', 7
					Union 
					Select '2H', 8
					Union 
					Select '2I', 9
				), ACAHistoryDetail AS
				(
						select rmh.HRCo, 
							   rmh.HRRef, 
							   rmh.SeriesTwoCode,
							   datepart(month, ActivityDate) as BeginningMonthNumber,
							   datepart(month,isnull(rmh.ExpirationDate, @EndOfTaxYear)) as EndingMonthNumber,
							   datepart(Year,isnull(rmh.ExpirationDate, @EndOfTaxYear)) as EndingYearNumber,
							   Row_Number() OVER (PARTITION BY rmh.HRCo, rmh. HRRef, datepart(month, ActivityDate) ORDER BY bfr.Ranking Asc) AS OfferRank
							from dbo.HRResourceMasterACAHistory rmh
							inner join SeriesTwoCodeRanking bfr
								on rmh.SeriesTwoCode = bfr.SeriesTwoCode
							where rmh.SeriesTwoCode is not null
							and datepart(year, ActivityDate) = @TaxYear
							group by
							   rmh.HRCo, 
							   rmh.HRRef, 
							   rmh.SeriesTwoCode,
							   rmh.ActivityDate,
							   rmh.ExpirationDate,
							   bfr.Ranking 
				), CoverageAcceptance AS
				(
					Select h.PRCo,
						   h.Employee,
						   x.SeriesTwoCode,
						   x.BeginningMonthNumber,
						   x.EndingMonthNumber,
						   x.EndingYearNumber,
						   cast('N' as char(1)) as IsCoveredForTaxYear,
						   r.Ranking OfferRank
					--partition series 2 intervals by month.
					from ACAHistoryDetail x
					inner join dbo.HRRM rm
						on rm.HRCo = x.HRCo 
						and rm.HRRef = x.HRRef
					inner join dbo.PR1095Header h
						on  rm.PRCo = h.PRCo
						and rm.PREmp = h.Employee
					inner join SeriesTwoCodeRanking r
						on r.SeriesTwoCode = x.SeriesTwoCode
					where x.OfferRank = 1
					  and h.PRCo = @PRCo
					  and h.TaxYear = @TaxYear
					  and exists (select 1 from #IncludedEmployees ie 
							               where ie.PRCo = h.PRCo 
							                 and ie.Employee = h.Employee 
								             and ie.TaxYear = h.TaxYear)
				)
				Update d set d.Sec4980HCode = o.SeriesTwoCode
						 from dbo.PR1095EmployeeDetail d   
						 inner join CoverageAcceptance o
							on o.Employee = d.Employee
							and d.PRCo = o.PRCo
						 inner join #Months m 
							on m.Name = d.TaxMonth
						 where d.PRCo = @PRCo 
							and	d.TaxYear = @TaxYear
							and m.Number between o.BeginningMonthNumber and o.EndingMonthNumber
							and o.OfferRank <= (Select max(Ranking) from SeriesTwoCodeRanking)
				            and exists (select 1 from #IncludedEmployees ie 
						                         where ie.PRCo = d.PRCo 
						                           and ie.Employee = d.Employee 
							                       and ie.TaxYear = d.TaxYear)
				
				
				--Apply similar logic to actual coverage indicated in ACA History and HREB. -

				;WITH EmployeeCoverageDetail AS
				(
					select edc.HRCo, 
					   edc.HRRef, 
					   edc.DependentID,
					   isnull(edc.ACAHealthCareYN, 'N') as ACAHealthCareYN,
					   isnull(edc.ACAMinEssenCoverage, 'N') AS	ACAMinEssenCoverage,
					   isnull(edc.ACASelfInsured, 'N') as ACASelfInsured,
					   isnull(edc.BenefitCode, 'No Coverage') as BenefitCode,
					   min(datepart(month, IsNull(edc.EffectDate, @StartOfTaxYear))) as BeginningMonthNumber,
					   max(datepart(month,isnull(edc.EndDate, @EndOfTaxYear))) as EndingMonthNumber,
					   max(datepart(Year,isnull(edc.EndDate, @EndOfTaxYear))) as EndingYearNumber
					from #EmployeeAndDependentCoverage edc
					where datepart(year, isnull(edc.EndDate, @EndOfTaxYear)) = @TaxYear
					  and edc.ACAHealthCareYN = 'Y'
					group by
					   edc.HRCo, 
					   edc.HRRef, 
					   edc.DependentID,
					   edc.ACAHealthCareYN,
					   edc.ACAMinEssenCoverage,
					   edc.ACASelfInsured,
					   edc.BenefitCode,
					   edc.EffectDate,
					   IsNull(edc.EndDate, @EndOfTaxYear)		
				), ActualCoverage AS	
				(					
					Select h.PRCo,
						   h.Employee,
						   x.DependentID,
						   x.ACAHealthCareYN,
						   x.ACAMinEssenCoverage,
						   x.ACASelfInsured,
						   x.BeginningMonthNumber,
						   x.EndingMonthNumber,
						   x.EndingYearNumber,
						   cast('N' as char(1)) as IsCoveredForTaxYear
					from EmployeeCoverageDetail x
					inner join dbo.HRRM rm
						on rm.HRCo = x.HRCo 
						and rm.HRRef = x.HRRef
					inner join dbo.PR1095Header h
						on  rm.PRCo = h.PRCo
						and rm.PREmp = h.Employee
					where  h.PRCo = @PRCo
					  and h.TaxYear = @TaxYear
				)
				update c set	c.Jan   = Case When 1 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.Feb   = Case When 2 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.Mar   = Case When 3 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.Apr   = Case When 4 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.May   = Case When 5 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.June  = Case When 6 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.July  = Case When 7 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.Aug   = Case When 8 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.Sept  = Case When 9 Between a.BeginningMonthNumber  and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.Oct   = Case When 10 Between a.BeginningMonthNumber and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.Nov   = Case When 11 Between a.BeginningMonthNumber and a.EndingMonthNumber Then 'Y' Else 'N' End,
								c.Dec	= Case When 12 Between a.BeginningMonthNumber and a.EndingMonthNumber Then 'Y' Else 'N' End
				from dbo.PR1095Covered c
				cross apply (select TOP (1) ac.BeginningMonthNumber, ac.EndingMonthNumber from ActualCoverage ac
							 where ac.PRCo = c.PRCo
							 and ac.Employee = c.Employee
							 and ac.DependentID = c.DependentID
							 and ac.ACASelfInsured = 'Y'
							 and ac.ACAHealthCareYN = 'Y') a
				where c.PRCo = @PRCo 
				  and c.TaxYear = @TaxYear
				  and not exists (select 1 from #ExcludedDependentRecords ecr 
				                           where ecr.Employee = c.Employee 
				  						   and ecr.PRCo = c.PRCo 
				  						   and ecr.TaxYear = c.TaxYear 
				  						   and ecr.DependentID = c.DependentID)

				Update d set d.EmployeeShareAmt = null
						from dbo.PR1095EmployeeDetail d   
						where exists (select 1 from #IncludedEmployees ie 
											   where ie.PRCo = d.PRCo 
												 and ie.Employee = d.Employee 
												 and ie.TaxYear = d.TaxYear)
							and d.PRCo = @PRCo 
							and d.TaxYear = @TaxYear
							and d.CoverageOfferCode not in('1B','1C','1D','1E')
														
							
		  End--Initi from HR = 'Y'



		/*
			Toggle Yearly flags in the header records depending on values in the detail to conform to IRS instructions regarding how the boxes should be checked.
		*/
				

		--These updates apply whether initializing from HR or PR.
		--If the same code exists for the entire year, write it out to the header record.			
		;With YearlyOfferCodeFromDetailHistory as
		(
			Select  d.PRCo, 
					d.Employee,
					d.TaxYear,
					d.CoverageOfferCode,
					count(*) as OfferCodeCount  
				from dbo.PR1095EmployeeDetail d
				where d.PRCo = @PRCo
				and d.TaxYear = @TaxYear
				group by 
					d.PRCo,
					d.Employee,
					d.TaxYear,
					d.CoverageOfferCode
				having count(*) = 12
		)
		--If there were mixed codes in a year yoc.CoverageOfferCode will be null and 
		--Yearly coverage offer will be updated to blank.
		Update h set h.OfferCoverageAll12Mths = IsNull(yoc.CoverageOfferCode, '') 	 
					from dbo.PR1095Header h
					inner join YearlyOfferCodeFromDetailHistory yoc
						on yoc.PRCo = h.PRCo
						and yoc.TaxYear = h.TaxYear
						and yoc.Employee = h.Employee
						--Only flip flags on included employees.
						and exists (select 1 from #IncludedEmployees ie 
									         where ie.PRCo = h.PRCo 
									           and ie.Employee = h.Employee 
										       and ie.TaxYear = h.TaxYear)


		--If the same code exists for the entire year, write it out to the header record.			
		;With YearlyAcceptanceCodeFromDetailHistory as
		(
			Select  d.PRCo, 
					d.Employee,
					d.TaxYear,
					d.Sec4980HCode,
					count(*) as OfferCodeCount  
				from dbo.PR1095EmployeeDetail d
				where d.PRCo = @PRCo
					and d.TaxYear = @TaxYear
				group by 
					d.PRCo,
					d.Employee,
					d.TaxYear,
					d.Sec4980HCode
				having count(*) = 12
		)
		--If there were mixed codes in a year yoc. will be null and 
		--Yearly coverage offer will be updated to blank.
		Update h set h.Section4980HAll12Mths = IsNull(yac.Sec4980HCode, '') 	 
					from dbo.PR1095Header h
					inner join YearlyAcceptanceCodeFromDetailHistory yac
						on yac.PRCo = h.PRCo
						and yac.TaxYear = h.TaxYear
						and yac.Employee = h.Employee
						--Only flip flags on included employees.
						and exists (select 1 from #IncludedEmployees ie 
									         where ie.PRCo = h.PRCo 
									           and ie.Employee = h.Employee 
										       and ie.TaxYear = h.TaxYear)


	    --If the same code exists for the entire year, write it out to the header record.			
		;With YearlyEmpShareAmountFromDetailHistory as
		(
			Select  d.PRCo, 
					d.Employee,
					d.TaxYear,
					d.EmployeeShareAmt,
					count(*) as OfferCodeCount  
				from dbo.PR1095EmployeeDetail d
				where d.PRCo = @PRCo
					and d.TaxYear = @TaxYear
				group by 
					d.PRCo,
					d.Employee,
					d.TaxYear,
					d.EmployeeShareAmt
				having count(*) = 12
		)
		--If there were mixed codes in a year yoc. will be null and 
		--Yearly coverage offer will be updated to blank.
		Update h set h.EmployeeShareAll12Mths = ysa.EmployeeShareAmt 
					from dbo.PR1095Header h
					inner join YearlyEmpShareAmountFromDetailHistory ysa	
					on ysa.PRCo = h.PRCo
						and ysa.TaxYear = h.TaxYear
						and ysa.Employee = h.Employee
						--Only flip flags on included employees.
						and exists (select 1 from #IncludedEmployees ie 
									         where ie.PRCo = h.PRCo 
									           and ie.Employee = h.Employee 
										       and ie.TaxYear = h.TaxYear)

		
		--Per IRS form instructions, blank out the details, if the header has been set
        Update d set d.CoverageOfferCode = ''
				from dbo.PR1095EmployeeDetail d
				inner join dbo.PR1095Header h		-- TFS-268516
				on h.Employee = d.Employee
					and h.TaxYear = d.TaxYear
					and h.PRCo = d.PRCo
				where d.PRCo = @PRCo
					and d.TaxYear = @TaxYear
					and IsNull(h.OfferCoverageAll12Mths, '') <> ''
					--Only flip flags on included employees.
					and exists (select 1 from #IncludedEmployees ie 
									     where ie.PRCo = d.PRCo 
									       and ie.Employee = d.Employee 
										   and ie.TaxYear = d.TaxYear)

		--Per IRS form instructions, blank out the details, if the header has been set
        Update d set d.Sec4980HCode = '' 
				from dbo.PR1095EmployeeDetail d
			    inner join dbo.PR1095Header h	-- TFS-268516
				on h.Employee = d.Employee
					and h.TaxYear = d.TaxYear
					and h.PRCo = d.PRCo
				where d.PRCo = @PRCo
					and d.TaxYear = @TaxYear
					and IsNull(h.Section4980HAll12Mths, '') <> ''
					--Only flip flags on included employees.
					and exists (select 1 from #IncludedEmployees ie 
									     where ie.PRCo = d.PRCo 
									       and ie.Employee = d.Employee 
										   and ie.TaxYear = d.TaxYear)

		--Per IRS form instructions, blank out the details, if the header has been set
        Update d set d.EmployeeShareAmt = null 
				from dbo.PR1095EmployeeDetail d
				inner join dbo.PR1095Header h		
				on h.Employee = d.Employee
					and h.TaxYear = d.TaxYear
					and h.PRCo = d.PRCo
				where d.PRCo = @PRCo
					and d.TaxYear = @TaxYear
					and IsNull(h.EmployeeShareAll12Mths, 0) <> 0
					--Only flip flags on included employees.
					and exists (select 1 from #IncludedEmployees ie 
									     where ie.PRCo = d.PRCo 
									       and ie.Employee = d.Employee 
										   and ie.TaxYear = d.TaxYear)


		--Toggle All12Months flag when coverage was offered every month.
		update c set c.CoveredAll12Months = 'Y',
					 c.Jan    = 'N',
					 c.Feb    = 'N',
					 c.Mar    = 'N',
					 c.Apr    = 'N',
					 c.May    = 'N',
					 c.June   = 'N',
					 c.July   = 'N',
					 c.Aug    = 'N',
					 c.Sept   = 'N',
					 c.Oct    = 'N',
					 c.Nov    = 'N',
					 c.[Dec]  = 'N' 
					from dbo.PR1095Covered c 
					where c.Jan  = 'Y' and
						c.Feb    = 'Y' and
						c.Mar    = 'Y' and
						c.Apr    = 'Y' and
						c.May    = 'Y' and
						c.June   = 'Y' and
						c.July   = 'Y' and
						c.Aug    = 'Y' and
						c.Sept   = 'Y' and
						c.Oct    = 'Y' and
						c.Nov    = 'Y' and
						c.[Dec]  = 'Y' 
					and c.TaxYear = @TaxYear 
					and PRCo = @PRCo
					and c.DependentID is not null
					--Only flip flags on included dependents.
					and not exists(select 1 from #ExcludedDependentRecords ecr 
					                        where ecr.TaxYear = c.TaxYear 
											  and ecr.Employee = c.Employee 
											  and ecr.PRCo = c.PRCo 
											  and ecr.DependentID = c.DependentID)

    Commit Transaction;				
END
GO
