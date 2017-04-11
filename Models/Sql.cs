namespace TransportManage.Models
{

    public static class Sql
    {
        public const string GetCompanyTaskCount = @"SELECT
	COUNT(*)
FROM TransportManage..DeliveryTask d
WHERE d.TaskStatus = 2";

        public const string GetDumpCheckNumber = @"SELECT
	f.FlowNumber
FROM TransportManage..CheckedTask ct
INNER JOIN TransportManage..Flow f
	ON f.FlowNumber = ct.FlowNumber
WHERE f.Name = '消纳审核'";
        public const string GetCheckedFlowMaxNumber = @"SELECT TOP 1
	f.FlowNumber
FROM TransportManage..Flow f
ORDER BY f.FlowNumber DESC";
        public const string VerifyIsFinsh = @"SELECT
	'ok'
FROM TransportManage..CheckedTask ct
INNER JOIN TransportManage..Flow f
	ON f.FlowNumber = ct.FlowNumber
WHERE f.Name = '审核完成' AND ct.TaskId=@taskId";
        public const string GetVerifyAuth = @"IF ((SELECT
	COUNT(*)
FROM TransportManage..CheckedTask ct
WHERE ct.taskId = @taskId) = 1)
BEGIN

SELECT
	'ok'
FROM TransportManage..FlowGroupDetail fgd
INNER JOIN TransportManage..EmployeeCorelation ec
	ON ec.DepartmentId = fgd.DepartmentId
INNER JOIN(

SELECT f.Id AS FlowId
from TransportManage..CheckedTask ct
INNER  join TransportManage..Flow f on f.FlowNumber=ct.FlowNumber+1
WHERE ct.TaskId=@taskId AND f.FlowType = '审核') t ON t.FlowId=fgd.FlowId
WHERE fgd.CompanyId = @CompanyId AND ec.EmployeeId = @EmployeeId
end
else
begin
SELECT
	'ok'
FROM TransportManage..FlowGroupDetail fgd
INNER JOIN TransportManage..EmployeeCorelation ec
	ON ec.DepartmentId = fgd.DepartmentId
INNER JOIN TransportManage..Flow f
	ON f.Id = fgd.FlowId
WHERE fgd.CompanyId = @CompanyId AND ec.EmployeeId = @EmployeeId AND f.FlowNumber = 1
end";
        public const string OrderDayResultSql = @"
IF (@type = '工地')
begin
SELECT
	w.Name ,
	v.PlateNumber,
	s.Name AS SoilType,
	od.WorkPrice as Price,
	COUNT(*) AS TotalCount,
			datename(year,v.UnloadingTime)+datename(month,v.UnloadingTime)+DATENAME(DAY,v.UnloadingTime)
	 As Time
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = v.OrderDetailId
INNER JOIN TransportManage..SoilType s
	ON s.Id = od.SoilTypeId
INNER JOIN TransportManage..WorkSite w
	ON w.Id = od.WorkSiteId
WHERE v.CompleteTime BETWEEN @startTime AND @endTime AND v.TaskStatus =4 AND v.CompanyId = @companyId and od.WorkSiteId=@workSiteId
GROUP BY	w.Name,
			v.PlateNumber,
			s.Name,
			od.WorkPrice,
			datename(year,v.UnloadingTime)+datename(month,v.UnloadingTime)+DATENAME(DAY,v.UnloadingTime)
			
end
else IF (@type = '消纳')
begin
SELECT
	k.Name,
	v.PlateNumber,
	s.Name AS SoilType,
	od.KillSitePrice as Price,
	COUNT(*) AS TotalCount,
			datename(year,v.UnloadingTime)+datename(month,v.UnloadingTime)+DATENAME(DAY,v.UnloadingTime)
	 As Time
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = v.OrderDetailId
INNER JOIN TransportManage..SoilType s
	ON s.Id = od.SoilTypeId
	inner join TransportManage..KillMudstoneSite k ON k.Id=od.KillMudstoneSiteId
WHERE v.CompleteTime BETWEEN @startTime AND @endTime AND v.TaskStatus = @taskStatus AND v.CompanyId = @companyId and od.WorkSiteId=@workSiteId
GROUP BY	k.Name,
			v.PlateNumber,
			s.Name,
			od.KillSitePrice,
			datename(year,v.UnloadingTime)+datename(month,v.UnloadingTime)+DATENAME(DAY,v.UnloadingTime)
end";
        public const string OrderMonthResultSql = @"
IF (@type = '工地')
begin
SELECT
	w.Name ,
	v.PlateNumber,
	s.Name AS SoilType,
	od.WorkPrice as Price,
	COUNT(*) AS TotalCount,
			datename(year,v.UnloadingTime)+datename(month,v.UnloadingTime)
	 As Time
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = v.OrderDetailId
INNER JOIN TransportManage..SoilType s
	ON s.Id = od.SoilTypeId
INNER JOIN TransportManage..WorkSite w
	ON w.Id = od.WorkSiteId
WHERE v.CompleteTime BETWEEN @startTime AND @endTime AND v.TaskStatus =4 AND v.CompanyId = @companyId and od.WorkSiteId=@workSiteId
GROUP BY	w.Name,
			v.PlateNumber,
			s.Name,
			od.WorkPrice,
			datename(year,v.UnloadingTime)+datename(month,v.UnloadingTime)
			
end
else IF (@type = '消纳')
begin
SELECT
	k.Name,
	v.PlateNumber,
	s.Name AS SoilType,
	od.KillSitePrice as Price,
	COUNT(*) AS TotalCount,
			datename(year,v.UnloadingTime)+datename(month,v.UnloadingTime)
	 As Time
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = v.OrderDetailId
INNER JOIN TransportManage..SoilType s
	ON s.Id = od.SoilTypeId
	inner join TransportManage..KillMudstoneSite k ON k.Id=od.KillMudstoneSiteId
WHERE v.CompleteTime BETWEEN @startTime AND @endTime AND v.TaskStatus = @taskStatus AND v.CompanyId = @companyId and od.WorkSiteId=@workSiteId
GROUP BY	k.Name,
			v.PlateNumber,
			s.Name,
			od.KillSitePrice,
			datename(year,v.UnloadingTime)+datename(month,v.UnloadingTime)
end";

        public const string RollBackTask = @"DELETE TransportManage..CheckedTask  WHERE TaskId=@taskId";

        public const string CheckApplicationSql = @"";

        public const string UpdateRoleAuth = @"";

        public const string GetOneFlowEmployeeData = @"SELECT
	e.Name,
	CASE
		WHEN t.EmployeeId IS NULL THEN 0 ELSE 1
	END AS IsTrue
FROM TransportManage..Employee e
LEFT JOIN (SELECT
	ec.EmployeeId
FROM TransportManage..Flow f
INNER JOIN TransportManage..FlowGroupDetail fgd
	ON fgd.FlowId = f.Id
INNER JOIN TransportManage..Department d
	ON d.Id = fgd.DepartmentId
INNER JOIN TransportManage..EmployeeCorelation ec
	ON ec.DepartmentId = d.Id
WHERE f.Id = @flowId AND f.FlowType = @type)
t
	ON t.EmployeeId = e.Id	";

        public const string GetAllFlowEmployeeData = @"
DECLARE @FlowData table (
Id int,
FlowNumber int,
Name nvarchar (50),
FlowType nvarchar (15)
);
INSERT @FlowData
	SELECT
		f.Id,
		f.FlowNumber,
		f.Name,
		f.FlowType
	FROM TransportManage..Flow f
	WHERE f.CompanyId = @CompanyId
	
SELECT
	*
FROM @FlowData

SELECT
	e.Name,
	CASE
		WHEN t.EmployeeId IS NULL THEN 0 ELSE 1
	END AS IsTrue
FROM TransportManage..Employee e
LEFT JOIN (SELECT
	ec.EmployeeId
FROM TransportManage..Flow f
INNER JOIN TransportManage..FlowGroupDetail fgd
	ON fgd.FlowId = f.Id
INNER JOIN TransportManage..Department d
	ON d.Id = fgd.DepartmentId
INNER JOIN TransportManage..EmployeeCorelation ec
	ON ec.DepartmentId = d.Id
WHERE f.Id IN (SELECT
	f.Id
FROM @FlowData f)
AND f.FlowType = @type)
t
	ON t.EmployeeId = e.Id";

        public const string GetOneEmployeeWorkSite = @"SELECT
	e.Id,
	e.Name,
	CASE
		WHEN t.EmployeeId IS NULL THEN 0 ELSE 1
	END AS IsTrue
FROM TransportManage..Employee e
LEFT JOIN (SELECT
	ec.EmployeeId
FROM TransportManage..ResourceAuthorization ra
INNER JOIN TransportManage..Department d
	ON d.Id = ra.DepartmentId
INNER JOIN TransportManage..EmployeeCorelation ec
	ON ec.DepartmentId = d.Id
WHERE ra.SiteId = @workSiteId
AND ra.Type = @type)
t
	ON t.EmployeeId = e.Id";
        public const string GetAllWorkSiteEmployee = @"DECLARE @WorkSiteData table (
Id int,
Name nvarchar (50)
);

INSERT @WorkSiteData
	SELECT
		w.Id,
		w.Name
	FROM TransportManage..WorkSite w
	WHERE w.CompanyId = @companyId

SELECT
	*
FROM @WorkSiteData

SELECT
	e.Id,
	e.Name,
	CASE
		WHEN t.EmployeeId IS NULL THEN 0 ELSE 1
	END AS IsTrue
FROM TransportManage..Employee e
LEFT JOIN (SELECT
	ec.EmployeeId
FROM TransportManage..ResourceAuthorization ra
INNER JOIN TransportManage..Department d
	ON d.Id = ra.DepartmentId
INNER JOIN TransportManage..EmployeeCorelation ec
	ON ec.DepartmentId = d.Id
WHERE ra.SiteId IN (SELECT
	wd.Id
FROM @WorkSiteData wd)
AND ra.Type = @type)
t
	ON t.EmployeeId = e.Id";
        public const string GetOneWorkSiteEmployee = @"";
        public const string AddDesignateAuth = @"DECLARE @GroupId int,
@Name nvarchar(50)
if ((SELECT
	COUNT(*)
FROM TransportManage..FlowGroupDetail fd
WHERE fd.FlowId = @FlowId AND fd.CompanyId = @companyId) > 0)
begin
SET @GroupId = (SELECT
	fd.DepartmentId
FROM TransportManage..FlowGroupDetail fd
WHERE fd.FlowId = @FlowId)
INSERT TransportManage..EmployeeCorelation (DepartmentId, EmployeeId)
	VALUES (@GroupId, @EmployeeId)
end
else
begin
set @Name=( select f.Name from TransportManage..Flow f where f.Id=@FlowId)+'组'
INSERT TransportManage..Department (Name, CompanyId, Type, GroupStatus)
	VALUES (@Name, @CompanyId, '流程', 0)
	
SET @GroupId = (SELECT
	CAST(SCOPE_IDENTITY() AS int))
	insert TransportManage..FlowGroupDetail (FlowId,DepartmentId,CompanyId) VALUES(@FlowId,@GroupId,@CompanyId)
INSERT TransportManage..EmployeeCorelation (DepartmentId, EmployeeId)
	VALUES (@GroupId, @EmployeeId)
end";
        public const string GetTaskPrice = @"SELECT
	od.KillSitePrice AS DumpPrice,
	od.WorkPrice AS WorkSitePrice,
	od.DriverPrice
FROM TransportManage..OrderDetail od
WHERE od.Id = @OrderDetailId";
        public const string ModifyOrderSql = @"";
        public const string AuthSql = @"DECLARE @GroupId int
IF exists (SELECT
	*
FROM TransportManage..Department d
WHERE d.Name = @Name AND d.CompanyId = @CompanyId)
begin
SET @GroupId = (SELECT
	de.Id
FROM TransportManage..Department de
WHERE de.Name = @Name AND de.CompanyId = @CompanyId)
end
else
begin
INSERT TransportManage..Department (Name, CompanyId, Type)
	VALUES (@Name, @CompanyId, @Type);
SET @GroupId = (SELECT
	CAST(SCOPE_IDENTITY() AS int));
end
INSERT TransportManage..EmployeeCorelation (DepartmentId, EmployeeId)
	VALUES (@GroupId, @EmployeeId)";
        public const string FinanceAuthSql = @"";
        public const string DumpCheckAuthSql = @"";
        public const string WorkSiteAdminAuthSql = @"";
        public const string DRoleAuthSql = @" insert TransportManage..ResourceAuthorization (DepartmentId,SiteId,Type) VALUES (@DepartmentId,@SiteId,@Type)";
        public const string NewroleAuthSql = @"INSERT TransportManage..Department (Name,CompanyId,Type	) VALUES (@Name,@CompanyId,'系统权限')";
        public const string GetFlowSql = @"SELECT
	f.Id,
	f.FlowNumber,
	f.Name
FROM TransportManage..Flow f
WHERE f.FlowType=@type AND f.FlowNumber>0
ORDER BY f.FlowNumber ASC  ";
        public const string DetFlowSql = @"DELETE TransportManage..Flow WHERE Id =@Id";
        public const string AddFlowSql = @"UPDATE TransportManage..Flow
SET Id = Id + 1
WHERE Name = @FlowName;
INSERT TransportManage..Flow (Id, Name)
	SELECT
		f.Id - 1,
		@Name AS Name
	FROM TransportManage..Flow f
	WHERE f.Name = @FlowName";
        public const string CheckedTask = @"if ((select COUNT(*) from TransportManage..CheckedTask ct WHERE ct.TaskId=@taskId) = 1)
UPDATE TransportManage..CheckedTask
SET FlowNumber = FlowNumber + 1
WHERE TaskId = @TaskId
else
INSERT TransportManage..CheckedTask (TaskId, FlowNumber)
	VALUES (@TaskId, 1)";

        public const string GetDepartmentId = @"SELECT d.Id
from TransportManage..Department d
WHERE d.CompanyId=@companyId AND d.Parentid=0";
        public const string GetPhoto = @"DECLARE @temp table (
Id int,
Lat float,
Lng float,
Address nvarchar (150),
Type nvarchar (15),
Name nvarchar (50)
)
INSERT @temp
	SELECT
		sp.Id,
		sp.Lat,
		sp.Lng,
		sp.Address,
		sp.Type,
		sp.Name
	FROM TransportManage..SitePictures sp
    order by sp.Id desc

SELECT
	*
FROM @temp

SELECT
	spd.SitePicturesId AS  Id,spd.Url
FROM TransportManage..SitePicturesDetail spd
WHERE spd.SitePicturesId IN (SELECT
	t.Id
FROM @temp t)";
        public const string InsertPhotosDetail = @"INSERT TransportManage..SitePicturesDetail (SitePicturesId,Url) VALUES(@SitePicturesId,@Url)";
        public const string InsertPhotos = @"INSERT TransportManage..SitePictures (Lat,Lng,Address,Type,Name) VALUES(@Lat,@Lng,@Address,@Type,@Name);select SCOPE_IDENTITY();";
        public const string GetIsPayTaskIdSql = @"SELECT
	pd.DeliveryTaskId
FROM PayDetail pd";
        public const string GetDriverData = @"";
        public const string GetCompleteTaskDetailSql = @"DECLARE @Temp table
(
WorkSiteId int,
WorkSite nvarchar (50),
TaskSum int,
WorkPrice int,
DriverPrice int,
ExtraCost int,
TaskSum2 int,
WorkPrice2 int,
DriverPrice2 int,
ExtraCost2 int
)

INSERT @Temp
	SELECT
		od.WorkSiteId,
		w.Name AS WorkSite,
		COUNT(CASE f.FlowNumber
			WHEN @MaxNumber THEN  v.TaskId
		END) AS TaskSum,
		SUM(CASE f.FlowNumber
			WHEN @MaxNumber THEN od.WorkPrice
		END) AS WorkPrice,
			SUM(CASE f.FlowNumber
			WHEN @MaxNumber THEN od.DriverPrice
		END) AS DriverPrice,
		SUM(CASE f.FlowNumber
			WHEN @MaxNumber THEN ec.Cost
		END) AS ExtraCost,
		COUNT(CASE f.FlowNumber
			WHEN @DumpCheckedNumber THEN v.TaskId
		END) AS TaskSum2,
		SUM(CASE f.FlowNumber
			WHEN @DumpCheckedNumber THEN od.WorkPrice
		END) AS WorkPrice2,
				SUM(CASE f.FlowNumber
			WHEN @DumpCheckedNumber THEN od.DriverPrice
		END) AS DriverPrice2,
		SUM(CASE f.FlowNumber
			WHEN @DumpCheckedNumber THEN ec.Cost
		END) AS ExtraCost2
	FROM TransportManage.dbo.DeliveryTaskView v
	INNER JOIN TransportManage..OrderDetail od
		ON od.Id = v.OrderDetailId
	INNER JOIN TransportManage..[Order] o
		ON o.Id = od.OrderId
	INNER JOIN TransportManage..WorkSite w
		ON w.Id = od.WorkSiteId
	LEFT JOIN TransportManage..CarExtraCost ec
		ON ec.TaskNum = v.TaskNumber
	INNER JOIN TransportManage..CheckedTask ct
		ON ct.TaskId = v.TaskId
	INNER JOIN TransportManage..Flow f
		ON f.FlowNumber = ct.FlowNumber
	WHERE (f.Name = '消纳审核' OR f.Name = '完成审核') /* condition */
	GROUP BY	od.WorkSiteId,
				w.Name

SELECT
	*
FROM @Temp

SELECT
	k.Name AS Dump,
	s.Name AS SoilType,
	od.WorkSiteId,
	App.dbo.Concatenate(v.TaskId, ',') AS TaskIdList,
	COUNT(CASE f.FlowNumber
		WHEN @MaxNumber THEN v.TaskId
	END) AS TaskCount,
	SUM(CASE f.FlowNumber
		WHEN @MaxNumber THEN od.DriverPrice
	END) AS DriverPrice,
	SUM(CASE f.FlowNumber
		WHEN @MaxNumber THEN ec.Cost
	END) AS ExtraCost,
	COUNT(CASE f.FlowNumber
		WHEN @DumpCheckedNumber THEN v.TaskId
	END) AS TaskCount2,
	SUM(CASE f.FlowNumber
		WHEN @DumpCheckedNumber THEN od.DriverPrice
	END) AS DriverPrice2,
	SUM(CASE f.FlowNumber
		WHEN @DumpCheckedNumber THEN ec.Cost
	END) AS ExtraCost2
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage..OrderDetail od
	ON od.Id = v.OrderDetailId
INNER JOIN TransportManage..WorkSite w
	ON w.Id = od.WorkSiteId
INNER JOIN TransportManage..[Order] o
	ON o.Id = od.OrderId
INNER JOIN TransportManage..KillMudstoneSite k
	ON k.Id = od.KillMudstoneSiteId
INNER JOIN TransportManage..SoilType s
	ON s.Id = od.SoilTypeId
LEFT JOIN TransportManage..CarExtraCost ec
	ON ec.TaskNum = v.TaskNumber
INNER JOIN TransportManage..CheckedTask ct
	ON ct.TaskId = v.TaskId
INNER JOIN TransportManage..Flow f
	ON f.FlowNumber = ct.FlowNumber
WHERE od.WorkSiteId IN (SELECT
	temp.WorkSiteId
FROM @Temp temp)
AND (f.Name = '消纳审核' OR f.Name = '完成审核') /* condition */
GROUP BY	k.Name,
			s.Name,
			od.WorkSiteId";
        public const string GetPayOrderSummary = @"SELECT
	v.PlateNumber,
	p.PayNumber,
	v.LoadingPos,
	v.UnloadingPos,
	v.DriverName,
	s.Name AS SoilType,
	p.PayStatus,
	od.Id AS OrderDetailId,
	SUM(od.DriverPrice) AS DriverPrice,
	SUM(ec.Cost) AS ExtraCost,
	COUNT(od.Id) AS Num,
	App.dbo.Concatenate(pd.DeliveryTaskId, ',') AS TaskIdList,
	App.dbo.Concatenate(CONVERT(varchar(100), v.LoadingTime, 20), ',') AS LoadingTimeList,
	App.dbo.Concatenate(CONVERT(varchar(100), v.UnloadingTime, 20), ',') AS UnloadingTimeList
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage..OrderDetail od
	ON od.Id = v.OrderDetailId
INNER JOIN TransportManage..SoilType s
	ON s.Id = od.SoilTypeId
LEFT JOIN TransportManage..CarExtraCost ec
	ON ec.TaskNum = v.TaskNumber
INNER JOIN TransportManage..PayDetail pd
	ON pd.DeliveryTaskId = v.TaskId
INNER JOIN TransportManage..Pay p
	ON p.Id = pd.PayId
WHERE p.Id = @payOrderId
GROUP BY	od.Id,
			v.DriverName,
			v.PlateNumber,
			v.LoadingPos,
			v.UnloadingPos,
			s.Name,
			v.DriverName,
			p.PayNumber,
			p.PayStatus";

        public const string InsertPayDetail = @"INSERT TransportManage..PayDetail(PayId,DeliveryTaskId) VALUES (@PayId,@DeliveryTaskId)";
        public const string InsertPay = @"insert TransportManage..Pay (PayNumber,Operator) VALUES (@PayNumber,@Operator);SELECT CAST(SCOPE_IDENTITY() as int);";
        public const string InsertCancelPayData = @"DECLARE @tempTable table (
PayId int,
PayNumber nvarchar (15),
Operator int,
CreateTime datetime,
CancelTime datetime,
DeliveryTaskIdString nvarchar (max)
);

INSERT @tempTable
	SELECT
		p.Id AS PayId,
		p.PayNumber,
		p.Operator,
		p.CreateTime,
		GETDATE() AS CancelTime,
		'' AS DeliveryTaskIdString
	FROM TransportManage..Pay p
	WHERE p.Id = @payOrderId;

UPDATE @tempTable
SET DeliveryTaskIdString = (SELECT
	App.dbo.Concatenate(pd.DeliveryTaskId, ',')
FROM TransportManage..PayDetail pd
INNER JOIN @tempTable t
	ON pd.PayId = t.PayId)
WHERE PayId = @payOrderId;

INSERT INTO TransportManage..PayTrash (PayId, PayNmuber, Operator, CreateTime, CancelTime, DeliveryTaskIdString)
	SELECT
		*
	FROM @tempTable";
        public const string DeletPaySql = @"DELETE TransportManage..PayDetail WHERE PayId=@payOrderId;";
        public const string GetFKNumber = @"declare
@x1 int,
@y1 char(8)
select @x1= COUNT(CharNo) from FlowNo where CAST(DT as date)=CAST(GETDATE() as date)
if(@x1=0)
begin
select @y1=convert(char(8),getdate(),112)
insert FlowNo(CharNo,DT) values(
'FK'+@y1+'0001'
,GETDATE())
end
else
begin
  insert FlowNo(CharNo,DT) values(
'FK'+cast((select MAX(CAST( SUBSTRING(charno,3,12) as bigint))+1 from FlowNo where CAST(DT as date)=CAST(GETDATE() as date))as varchar(50))
,GETDATE())
end
select TOP(1)CharNo from FlowNo ORDER by Dt DESC
";
        public const string GetBDNumber = @"declare
    @x1 int,
    @y1 char (8)
    SELECT
	    @x1 = COUNT(CharNo)
    FROM FlowNo
    WHERE CAST(DT AS date) = CAST(GETDATE() AS date)
    if (@x1 = 0)
    begin
    SELECT
	    @y1 = CONVERT(char(8), GETDATE(), 112)
    INSERT FlowNo (CharNo, DT)
	    VALUES ('BD' + @y1 + '0001', GETDATE())
    end
    else
    begin
    INSERT FlowNo (CharNo, DT)
	    VALUES ('BD' + CAST((SELECT
		    MAX(CAST(SUBSTRING(charno, 3, 12) AS bigint)) + 1
	    FROM FlowNo
	    WHERE CAST(DT AS date) = CAST(GETDATE() AS date))
	    AS varchar(50)), GETDATE())
    end
    SELECT TOP (1)
	    CharNo
    FROM FlowNo
    ORDER BY Dt DESC
";
        public const string VerifyOrderDetailSql = @"SELECT
	od.Id
FROM TransportManage..OrderDetail od
WHERE od.OrderId = @orderId AND od.Id = @orderDetailId";
        public const string InsertBDActionLog = @"INSERT TransportManage..ReplenishOrder (TaskId,OperatorId,Remark) VALUES (@TaskId,@OperatorId,@Remark)";
        public const string VerifyOrderIsExpiresSql = @"SELECT
	o.OrderStatus
FROM [TransportManage].[dbo].[Order] o
WHERE Id = @OrderId";
        public const string OperatePayOrderSql = @"update TransportManage..Pay SET Paystatus=@status WHERE Id=@payOrderId AND Paystatus=0";
        public const string InsertTask = @"INSERT TransportManage..DeliveryTask (TaskStatus,OrderDetailId,DriverCarId,CompanyId,TaskNumber) VALUES(@TaskStatus,@OrderDetailId,@DriverCarId,@CompanyId,@TaskNumber);SELECT CAST(SCOPE_IDENTITY() as int);";
        public const string GetDriverCarIdSql = @"SELECT
	dc.Id
FROM TransportManage..DriverCar dc
INNER JOIN TransportManage..Car c
	ON c.Id = dc.CarId
INNER JOIN TransportManage..Employee e
	ON e.Id = dc.EmployeeId
WHERE e.Name = @driverName AND c.PlateNumber = @plateNumber";
        public const string GetOrderNumberSql = @"declare
@x1 int,
@y1 char (8)
SELECT
	@x1 = COUNT(CharNo)
FROM FlowNo
WHERE CAST(DT AS date) = CAST(GETDATE() AS date)
if (@x1 = 0)
begin
SELECT
	@y1 = CONVERT(char(8), GETDATE(), 112)
INSERT FlowNo (CharNo, DT)
	VALUES (@Head + @y1 + '0001', GETDATE())
end
else
begin
INSERT FlowNo (CharNo, DT)
	VALUES (@Head + CAST((SELECT
		MAX(CAST(SUBSTRING(charno, 3, 12) AS bigint)) + 1
	FROM FlowNo
	WHERE CAST(DT AS date) = CAST(GETDATE() AS date))
	AS varchar(50)), GETDATE())
end
SELECT TOP (1)
	CharNo
FROM FlowNo
ORDER BY Dt DESC";
        public const string InsertReplenishmentOrderSql = @"";
        public const string GetPayOrderDetail = @"SELECT
	v.UnloadingTime,v.PlateNumber,
	p.PayNumber,
	v.LoadingPos,
	v.UnloadingPos,
	v.DriverName,
	v.TaskNumber,
	s.Name AS SoilType,
	od.DriverPrice as DriverPrice,
	ec.Cost AS Cost
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage..OrderDetail od
	ON od.Id = v.OrderDetailId
INNER JOIN TransportManage..SoilType s
	ON s.Id = od.SoilTypeId
LEFT JOIN TransportManage..CarExtraCost ec
	ON ec.TaskNum = v.TaskNumber
INNER JOIN TransportManage..PayDetail pd
	ON pd.DeliveryTaskId = v.TaskId
INNER JOIN TransportManage..Pay p
	ON p.Id = pd.PayId
	WHERE p.Id=@payOrderId";
        public const string GetPayOrder = @"SELECT
	p.PayNumber,
	p.PayStatus,
	e.Name AS DriverName,
	c.PlateNumber,
	p.Id AS PayOrderId,
	SUM(od.DriverPrice) AS DriverPrice,SUM(ec.Cost) AS Cost
FROM [TransportManage].[dbo].[Pay] p
INNER JOIN TransportManage..PayDetail pd
	ON pd.PayId = p.Id
INNER JOIN TransportManage..DeliveryTask d
	ON d.Id = pd.DeliveryTaskId
INNER JOIN TransportManage..DriverCar dc
	ON dc.Id = d.DriverCarId
INNER JOIN TransportManage..Car c
	ON c.Id = dc.CarId
INNER JOIN TransportManage..Employee e
	ON e.Id = dc.EmployeeId
	inner join TransportManage..OrderDetail od on od.Id=d.OrderDetailId
	left join TransportManage..CarExtraCost ec on ec.TaskNum=d.TaskNumber
	WHERE p.PayNumber LIKE @PayOrderNumber AND c.PlateNumber LIKE @PlateNumber AND e.Name LIKE @DriverName
	GROUP by p.PayNumber,p.PayStatus,e.Name,c.PlateNumber,p.Id";
        public const string Back = @"";
        public const string Affirm = @"";
        public const string UpdatePayOrder = @"";

        public const string VerifyTask = @"  SELECT
	t.DriverCarId
FROM [TransportManage].[dbo].[DeliveryTask] t
WHERE t.Id IN (@taskId) AND t.TaskStatus=4
  GROUP by t.DriverCarId,t.TaskStatus";
        public const string UpdateCar = @"UPDATE TransportManage..Car SET PlateNumber=@PlateNumber WHERE Id=@carId";
        public const string DeleteRole = @"DELETE TransportManage..EmployeeLevel WHERE EmployeeId IN(@employeeId) AND Name=@roleName";
        public const string GetRoleMember = @"SELECT
	e.Id,
	e.Name,
	e.Tel
FROM TransportManage..EmployeeLevel el
INNER JOIN TransportManage..Employee e
	ON e.Id = el.EmployeeId
WHERE e.CompanyId = @companyId AND el.Name = @RoleName";
        public const string GetRoleList = @"SELECT DISTINCT
	el.Name,
	(SELECT
		c.Name
	FROM TransportManage..Company c
	WHERE c.Id = @companyId)
	AS CompanyName
FROM TransportManage..EmployeeLevel el
INNER JOIN TransportManage..Employee e
	ON e.Id = el.EmployeeId
WHERE e.CompanyId = @companyId AND el.Name != '工管' AND el.Name != '司机'";
        public const string DelAuthority = @"DELETE TransportManage..EmployeeLevel WHERE EmployeeId=@employeeId";
        public const string InsertEmployeeCar = @"DECLARE @temp table (id int)
INSERT TransportManage..Car (PlateNumber, CompanyId)
OUTPUT INSERTED.Id INTO @temp
	VALUES (@PlateNumber, @CompanyId)

INSERT TransportManage..DriverCar (EmployeeId, CarId)
	VALUES (@EmployeeId, (SELECT
		t.id
	FROM @temp t)
	)
";
        public const string AddAuthority = @"
INSERT TransportManage..EmployeeLevel (Name, EmployeeId)
	VALUES (@roleName, @EmployeeId)";
        public const string IsDriverDepartment = @"SELECT
	count(*)
FROM TransportManage..Department
WHERE Id = @deptId AND AuthorityStatus = 1";
        public const string Insertemployee = @"DECLARE @temp table (id int)
INSERT TransportManage..Employee (Name, Tel, Password, CompanyId)
OUTPUT INSERTED.Id INTO @temp
	VALUES (@Name, @Tel, @Password, @CompanyId)

INSERT TransportManage..EmployeeCorelation (DepartmentId, EmployeeId)
	VALUES (@deptId, (SELECT
		t.id
	FROM @temp t)
	)
	SELECT * FROM @temp";
        public const string UpdateEmployeeLevel = @"";
        public const string GetDepartmentEmployee = @"SELECT TOP 1
	'ok'
FROM TransportManage..EmployeeCorelation ec
WHERE ec.DepartmentId = @id";
        public const string GetExpireAccount = @"SELECT
	COUNT(*)
FROM TransportManage..Employee e
WHERE e.DueTime <= GETDATE() AND e.Id = @companyId";
        public const string DelEmployee = @"";
        public const string GetCompanyOrder = @"SELECT
	'ok'
FROM TransportManage..[Order] o
WHERE o.Id = @orderId AND o.CompanyId = @companyId";
        public const string DelEmployeeLevel = @"DELETE TransportManage..EmployeeLevel WHERE EmployeeId=@id";
        public const string GetIsDriver = @"SELECT 'true' FROM TransportManage..EmployeeLevel e WHERE e.EmployeeId=@id AND e.Name='司机'";
        public const string SelectAuthority = @"SELECT
	'true'
FROM TransportManage..Department d
WHERE d.Id
IN (@id) AND d.AuthorityStatus = 1";
        public const string InsertEmployeeDepartment = @"INSERT TransportManage..EmployeeCorelation (DepartmentId,EmployeeId) VALUES (@DepartmentId,@EmployeeId)";
        public const string DelEmployDepartment = @"DELETE TransportManage..EmployeeCorelation WHERE EmployeeId in(@id)";
        public const string UpdateEmployee = @"UPDATE TransportManage..Employee SET Name=@name,Tel=@tel,Password=@password WHERE Id=@Id";
        public const string GetCompanyName = @"SELECT c.Name
from TransportManage..Company c
WHERE c.Id=@companyId";
        public const string GetDepartMentList = @";WITH Test_CTE as
(
	SELECT d.Id,d.Parentid,d.Name ,-1 AS lv1 from TransportManage..Department d
	WHERE d.CompanyId=@companyId AND d.Parentid=0
	UNION ALL
	SELECT d.Id,d.Parentid,d.Name, CASE WHEN lv1=0 THEN d.Parentid ELSE lv1+1 END AS lv1  from Test_CTE c inner join TransportManage..Department d ON c.Id=d.Parentid
	
)select e.Id,e.Name,e.Parentid from Test_CTE e
WHERE e.Parentid=@id";
        public const string GetEmployee = @"SELECT DISTINCT
d.Id,
	t.EmployeeId,
	t.Tel,
	t.Name,
	t.[Password],
	(SELECT
		c.PlateNumber
	FROM TransportManage..DriverCar dc
	INNER JOIN TransportManage..Car c
		ON c.Id = dc.CarId
	WHERE dc.EmployeeId = t.EmployeeId)
	AS
	PlateNumber
FROM TransportManage..Department d
INNER JOIN (SELECT
	ec.DepartmentId,
	e.Name,
	e.Tel,
	e.[Password],
	e.Id AS EmployeeId
FROM TransportManage..Employee e
LEFT JOIN TransportManage..EmployeeCorelation ec
	ON e.Id = ec.EmployeeId)
t
	ON t.DepartmentId = d.Id
WHERE d.Id=@departmentID AND d.CompanyId=@companyId";

        public const string GetSubDept = @"SELECT [Id]
      ,[Name]
  FROM [TransportManage].[dbo].[Department]
WHERE Parentid=@Id";
        public const string DelDept = @"  DELETE TransportManage..Department WHERE Id=@Id AND AuthorityStatus!=1";
        public const string AddDept = @"if exists (SELECT
	*
FROM TransportManage..Department
WHERE Id = @parentid AND AuthorityStatus = 1)
begin
INSERT TransportManage..Department (Name, Parentid, AuthorityStatus,CompanyId)
	VALUES (@Name, @Parentid, 1,@CompanyId)
end
else
begin
INSERT TransportManage..Department (Name, Parentid,CompanyId)
	VALUES (@Name, @Parentid,@CompanyId)
end";
        public const string GetAllDriver = @"SELECT
	DISTINCT e.Name
FROM TransportManage..Employee e
WHERE  e.CompanyId=2
ORDER BY e.Name";
        public const string GetAllVehicleNumber = @"SELECT
	c.PlateNumber
FROM TransportManage..Car c
where c.CompanyId=@companyId
ORDER BY c.PlateNumber";
        public const string GetAdminStaticCondition = @"AND od.WorkSiteId IN (SELECT
	wag.WorkSiteId
FROM TransportManage.dbo.WorkSiteAdminGroup wag
WHERE wag.EmployeeId = @EmployeeId UNION SELECT
	sgd.WorkSiteId
FROM TransportManage.dbo.Employee e
INNER JOIN TransportManage.dbo.EmployeeCorelation ec
	ON e.Id = ec.EmployeeId
INNER JOIN TransportManage.dbo.ShareGroupDetail sgd
	ON sgd.ShareDepartmentId = ec.DepartmentId
WHERE e.Id = @EmployeeId)";
        public const string InsertEmployeeLevel = @"INSERT TransportManage..EmployeeLevel (Name,EmployeeId) VALUES (@Name,@EmployeeId)";
        public const string InsertDepartment = @"insert [TransportManage].[dbo].[EmployeeCorelation] (DepartmentId,EmployeeId) VALUES (@DepartmentId,@EmployeeId)";
        public const string UpdateOpenAdminOrderDetail = @"  UPDATE TransportManage.dbo.OrderDetail SET OrderDetailStatus=1,Operater=@EmployeeId WHERE Id in @OrderDetailId AND OrderDetailStatus=2 AND CompanyId=@companyId";
        public const string UpdateCloseAdminOrderDetail = @"  UPDATE TransportManage.dbo.OrderDetail SET OrderDetailStatus=2,Operater=@EmployeeId WHERE Id in @OrderDetailId AND OrderDetailStatus=1 AND CompanyId=@companyId";
        public const string GetAdminDriver = @"SELECT DISTINCT
	e.Id
FROM TransportManage.dbo.OrderDetail od
INNER JOIN TransportManage.dbo.DeliveryTask dt
	ON dt.OrderDetailId = od.Id
INNER JOIN TransportManage.dbo.DriverCar dc
	ON dc.Id = dt.DriverCarId
INNER JOIN TransportManage.dbo.Employee e
	ON e.Id = dc.EmployeeId
WHERE od.CompanyId =@companyId AND od.WorkSiteId IN (SELECT
	wag.WorkSiteId
FROM TransportManage.dbo.WorkSiteAdminGroup wag
WHERE wag.EmployeeId = @EmployeeId UNION SELECT
	sgd.WorkSiteId
FROM TransportManage.dbo.Employee e
INNER JOIN TransportManage.dbo.EmployeeCorelation ec
	ON e.Id = ec.EmployeeId
INNER JOIN TransportManage.dbo.ShareGroupDetail sgd
	ON sgd.ShareDepartmentId = ec.DepartmentId
WHERE e.Id = @EmployeeId)";

        public const string GetShortcutDetailSql = @"SELECT
	v.LoadingRemark,
	v.UnloadingRemark,
	v.IsExistEvidence,
	v.LoadingTaskId,
	v.UnloadingTaskId,
	v.TaskId,
	(SELECT
		SUM(CC.Cost)
	FROM TransportManage.dbo.CarExtraCost cc
	WHERE cc.TaskNum = v.TaskNumber)
	AS ExtraCost,
	v.TaskNumber,
	v.LoadingPos AS Name,
	v.UnloadingPos AS Dump,
	v.PlateNumber,
	od.DriverPrice,
	v.LoadingTime,
	(SELECT
		st.Name
	FROM TransportManage.dbo.SoilType st
	WHERE st.Id = od.SoilTypeId)
	AS SoilType,
	v.UnloadingTime AS UnloadTime,
	od.KillSitePrice AS DumpPrice,
	od.WorkPrice AS WorkSitePrice,
	v.DriverName AS Driver,
	v.LoadingAddress,
	v.UnloadingAddress
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = v.OrderDetailId
INNER JOIN TransportManage..CheckedTask ct
	ON ct.TaskId = v.TaskId
INNER JOIN TransportManage..Flow f
	ON f.FlowNumber = ct.FlowNumber
LEFT JOIN TransportManage.dbo.WorkSite w
	ON w.Id = od.WorkSiteId
LEFT JOIN TransportManage.dbo.KillMudstoneSite k
	ON k.Id = od.KillMudstoneSiteId

WHERE v.DriverName LIKE @DriverName AND w.Name LIKE @Worksite AND v.PlateNumber LIKE @PlateNumber AND k.Name LIKE @Dump AND v.CompleteTime BETWEEN @startTime AND @endTime AND ct.FlowNumber = @flowNumber AND v.CompanyId = @companyId
ORDER BY v.CompleteTime DESC;";
        public const string GetAllWorksiteId = @"SELECT
	w.Id,
	w.Name
FROM TransportManage.dbo.WorkSite w
where w.CompanyId=@companyId";
        public const string GetShareGroup = @"";
        public const string GetOrderId = @"SELECT o.Id
from TransportManage.dbo.[Order] o
WHERE o.Operater =@userId";
        public const string DeleteEmployeeLevel = @"DELETE TransportManage.dbo.EmployeeLevel WHERE EmployeeId = @EmployeeId AND Name='工管'";
        public const string SignEmployee = @"INSERT TransportManage.dbo.EmployeeLevel (Name,EmployeeId) VALUES('工管',@EmployeeId)";
        public const string GetAllRemark = @"SELECT
	ec.Id,
	ec.TaskNum,
	CONVERT(varchar(100),ec.Date, 23) AS Date,
	ec.Createdate,
	ec.Remark,
	ec.Cost,
	(SELECT
		d.Name
	FROM TransportManage.dbo.DeliveryTask t
	INNER JOIN TransportManage.dbo.DriverCar dc
		ON dc.Id = t.DriverCarId
	INNER JOIN TransportManage.dbo.Employee d
		ON d.Id = dc.EmployeeId
	WHERE t.TaskNumber = ec.TaskNum)
	AS Driver
FROM TransportManage.[dbo].[CarExtraCost] ec
where ec.CompanyId=@companyId
ORDER BY ec.Createdate DESC";
        public const string AddWorkSiteAdmin = @"INSERT TransportManage.dbo.WorkSiteAdminGroup (EmployeeId,WorkSiteId) VALUES (@EmployeeId,@WorkSiteId)";
        public const string DeleteWorkSiteAdmin = @"DELETE TransportManage.dbo.WorkSiteAdminGroup WHERE EmployeeId=@employeeId AND WorkSiteId=@worksiteId AND CompanyId=@companyId";
        public const string GetWorkSiteAdmin = @"DECLARE @DepartmentData table (
Id int,
Name nvarchar (50)
);
INSERT @DepartmentData
	SELECT
		d.Id,
		d.Name
	FROM TransportManage.dbo.Department d
	WHERE d.CompanyId=@companyId

SELECT
	*
FROM @DepartmentData

SELECT
	ec.DepartmentId,
	e.Id AS EmployeeId,
	e.Name AS EmployeeName,
	CASE
		WHEN (SELECT
			w.Id
		FROM TransportManage.dbo.WorkSiteAdminGroup w
		WHERE w.EmployeeId = e.Id AND w.WorkSiteId = @worksiteId)
		IS NOT NULL THEN 2 ELSE 1
	END AS IsAdmin
FROM TransportManage.dbo.Employee e
LEFT JOIN TransportManage.dbo.EmployeeCorelation ec
	ON ec.EmployeeId = e.Id
WHERE ec.DepartmentId IN (SELECT
	d.Id
FROM @DepartmentData d)";

        public const string GetWorkSiteShare = @"SELECT
	d.Name,
	d.Id AS DepartmenId,
	CASE
		WHEN sgd.WorkSiteId = @worksiteId THEN 2 ELSE 1
	END AS IsShare
FROM TransportManage.dbo.Department d
LEFT JOIN TransportManage.dbo.ShareGroupDetail sgd
	ON d.Id = sgd.ShareDepartmentId
where d.CompanyId=@CompanyId
";
        public const string DelWorkSiteShare = @"DELETE TransportManage.dbo.ShareGroupDetail WHERE ShareDepartmentId=@departmentId AND WorkSiteId=@worksiteId AND CompanyId=@companyId";

        public const string GetAdminDump = @"SELECT DISTINCT
	od.KillMudstoneSiteId
FROM TransportManage.dbo.OrderDetail od
INNER JOIN (SELECT
	wag.WorkSiteId
FROM TransportManage.dbo.WorkSiteAdminGroup wag
WHERE wag.EmployeeId = @EmployeeId UNION SELECT
	sgd.WorkSiteId
FROM TransportManage.dbo.Employee e
INNER JOIN TransportManage.dbo.EmployeeCorelation ec
	ON e.Id = ec.EmployeeId
INNER JOIN TransportManage.dbo.ShareGroupDetail sgd
	ON sgd.ShareDepartmentId = ec.DepartmentId
WHERE e.Id = @EmployeeId)
t
	ON t.WorkSiteId = od.WorkSiteId
where od.CompanyId=@CompanyId";
        public const string GetAdminWorksite = @"SELECT
	w.Id,
	w.Name
FROM (SELECT
	wag.WorkSiteId
FROM TransportManage.dbo.WorkSiteAdminGroup wag
WHERE wag.EmployeeId = @EmployeeId UNION SELECT
	sgd.WorkSiteId
FROM TransportManage.dbo.Employee e
INNER JOIN TransportManage.dbo.EmployeeCorelation ec
	ON e.Id = ec.EmployeeId
INNER JOIN TransportManage.dbo.ShareGroupDetail sgd
	ON sgd.ShareDepartmentId = ec.DepartmentId
WHERE e.Id = @EmployeeId)
t
INNER JOIN TransportManage.dbo.WorkSite w
	ON w.Id = t.WorkSiteId
where w.CompanyId=@id";
        public const string DelSoilType = @"UPDATE TransportManage.dbo.SoilType SET TypeStatus =1 WHERE Id=@id AND CompanyId=@companyId";
        public const string ModifySoilType = @"UPDATE TransportManage.dbo.SoilType SET Name=@name WHERE Id=@id AND CompanyId=@companyId";
        public const string AddSoilType = @"INSERT TransportManage.dbo.SoilType (Name,CompanyId,Operater) VALUES(@Name,@CompanyId,@Operater)";
        public const string GetOrderSoilTypeId = @"SELECT st.Id FROM TransportManage.dbo.SoilType st WHERE st.Name=@soiltype AND st.CompanyId=@companyId";
        public const string GetOrderDumpId = @"SELECT ks.Id FROM TransportManage.dbo.KillMudstoneSite ks WHERE ks.Name=@dump AND ks.CompanyId=@companyId";
        public const string UpdateOrderDumpStatus = @"IF exists (SELECT
	*
FROM (SELECT
	wag.EmployeeId,
	od.Id AS OrderDetailId,od.CompanyId
FROM TransportManage.dbo.OrderDetail od
INNER JOIN TransportManage.dbo.WorkSiteAdminGroup wag
	ON wag.WorkSiteId = od.WorkSiteId UNION SELECT
	e.Id AS EmployeeId,
	od.Id AS OrderDetailId,od.CompanyId
FROM TransportManage.dbo.Employee e
INNER JOIN TransportManage.dbo.EmployeeCorelation ec
	ON e.Id = ec.EmployeeId
INNER JOIN TransportManage.dbo.ShareGroupDetail sgd
	ON sgd.ShareDepartmentId = ec.DepartmentId
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.WorkSiteId = sgd.WorkSiteId)
t
WHERE t.OrderDetailId = @orderDetailId AND t.EmployeeId = @EmployeeId AND t.CompanyId=@companyId)
BEGIN
UPDATE TransportManage.dbo.OrderDetail
SET OrderDetailStatus = @updateDumpStatus,Operater=@EmployeeId
WHERE Id IN @orderDetailId
SELECT
	'succeed'
end 
else
begin
SELECT
	'fail'
end";
        public const string GetOrderInfo = @"SELECT * 
FROM TransportManage.dbo.OrderDetail od 
WHERE od.OrderId=@orderId;";
        public const string AddOrderDetailSql = @"INSERT TransportManage.dbo.OrderDetail (KillMudstoneSiteId,KillSitePrice,WorkSiteId,WorkPrice,SoilTypeId,CompanyId,BossPrice,OrderId,DriverPrice,Operater) VALUES (@KillMudstoneSiteId,@KillSitePrice,@WorkSiteId,@WorkPrice,@SoilTypeId,@CompanyId,@BossPrice,@OrderId,@DriverPrice,@Operater);select scope_identity()";
        public const string GetDriverPhoto = @"SELECT
	tp.ImageUrl,td.TaskStep
FROM TransportManage.dbo.TaskPhoto tp
INNER JOIN TransportManage.dbo.DeliveryTaskDetail td
	ON td.Id = tp.TaskDetailId
WHERE td.DeliveryTaskId = @taskId";

        public const string UpdateOrderStatus = @"UPDATE TransportManage.dbo.[Order] SET OrderStatus=@orderStatus,Operater=@EmployeeId WHERE Id in @orderId AND CompanyId=@companyId";

        public const string InsertDialogue = @"INSERT TransportManage.dbo.Dialogue (Remark,UserId,UserType,TaskId,CompanyId) VALUES (@Remark,@UserId,@UserType,@TaskId,@CompanyId);SELECT CAST(SCOPE_IDENTITY() as int);";
        public const string InsertEvidence = @"INSERT TransportManage.dbo.EvidencePhoto (TaskId,Url,DialogueId) VALUES (@TaskId,@Url,@DialogueId)";
        public const string GetDialogue = @"DECLARE @Dialogue TABLE (
Id int,
Remark nvarchar (50),
CreateTime datetime,
TaskId int,
Name nvarchar (50),
DialogueType int
);

INSERT INTO @Dialogue
	SELECT
		*
	FROM (
	SELECT
		dl.Id,
		dl.Remark,
		dl.CreateTime,
		dl.TaskId,
		d.Name,dl.UserType AS DialogueType
	FROM TransportManage.dbo.Dialogue dl
	INNER JOIN TransportManage.dbo.Employee d
		ON d.Id = dl.UserId
	)
	t
	WHERE t.TaskId = @TaskId
	ORDER BY t.CreateTime desc

SELECT
	*
FROM @Dialogue;

SELECT
	e.Url,e.DialogueId
FROM TransportManage.dbo.EvidencePhoto e
WHERE e.DialogueId IN (SELECT
	d.Id
FROM @Dialogue d)
";
        public const string UpdateDeliveryTask = @"UPDATE TransportManage.dbo.DeliveryTask SET TaskStatus=2,IsExistEvidence=1 WHERE Id=@taskId AND TaskStatus=-1";
        public const string ReturnTaskSql = @"UPDATE TransportManage.dbo.DeliveryTask  SET TaskStatus=-1 WHERE Id in(@taskId) AND TaskStatus=2 AND CompanyId=@companyId";
        public const string GetLoglatAddress = @"SELECT GI.amap.GetAddress2(@lng,@lat,1)";
        public const string GetTaskNumStatus = @"SELECT
	t.TaskStatus
FROM TransportManage.dbo.DeliveryTask t
WHERE t.TaskNumber IN @taskNumList";
        public const string GetDriverFinishOrder2 = @"SELECT TOP 1000
	*,
	od.WorkPrice,
	od.KillSitePrice AS DumpPrice,
	s.Name AS SoilType,
	t.LoadingAddress,
	t.UnloadingAddress,
	ec.Cost AS ExtraCost,f.Name as FlowName
FROM TransportManage.dbo.DeliveryTaskView t
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = t.OrderDetailId
INNER JOIN TransportManage.dbo.SoilType s
	ON od.SoilTypeId = s.Id
LEFT JOIN TransportManage..CarExtraCost ec
	ON ec.TaskNum = t.TaskNumber
	inner join TransportManage..CheckedTask ct
	on ct.TaskId=t.TaskId
	inner join TransportManage..Flow f on f.FlowNumber=ct.FlowNumber
WHERE (f.FlowNumber = @FlowNumber /*condition*/) AND t.DriverId = @DriverId AND t.CompanyId = @CompanyId 
ORDER BY t.CreateTime DESC

";
        public const string GetFinishOrder2 = @"SELECT TOP 1000
	*,
	od.WorkPrice,
	od.KillSitePrice AS DumpPrice,
	s.Name AS SoilType,
	t.LoadingAddress,
	t.UnLoadingAddress,f.Name as FlowName
FROM TransportManage.dbo.DeliveryTaskView t
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = t.OrderDetailId
INNER JOIN TransportManage.dbo.SoilType s
	ON od.SoilTypeId = s.Id
INNER JOIN TransportManage..CheckedTask ct
	ON ct.TaskId = t.TaskId
INNER JOIN TransportManage..Flow f
	ON f.FlowNumber = ct.FlowNumber
WHERE (ct.FlowNumber = @flowNumber /*condition*/) AND t.CompanyId = @companyId /*andCondition*/
ORDER BY t.CreateTime DESC";
        public const string GetTaskDetailPhoto = @"SELECT tp.TaskDetailId, tp.ImageUrl
FROM TransportManage.dbo.TaskPhoto tp
WHERE tp.TaskDetailId IN (@taskDetailId)";
        public const string InsertTaskMultiplePhoto = @"INSERT TransportManage.dbo.TaskPhoto (TaskDetailId,ImageUrl) VALUES(@TaskDetailId,@ImageUrl)";
        public const string InsertDriverCar = @"  INSERT TransportManage.dbo.DriverCar (EmployeeId,CarId) VALUES(@EmployeeId,@CarId) ";
        public const string GetDingdingLogin = @"SELECT
	    t.UserType,t.Id,t.CompanyId
    FROM (SELECT
	    c.Id,
	    c.Name,
	    c.Password,
	    'Boss' AS UserType,
	    c.Id AS CompanyId
    FROM TransportManage.dbo.Company c UNION SELECT
	    d.Id,
	    d.Name,
	    d.Password,
	    'Driver' AS UserType,
	    d.CompanyId 
    FROM TransportManage.dbo.Employee d)
    t
    WHERE t.Name = @userName AND t.Password = @Password";
        public const string GetRemark = @"SELECT
	ec.Id,
	ec.TaskNum,
	CONVERT(varchar(100),ec.Date, 23) AS Date,
	ec.Createdate,
	ec.Remark,
	ec.Cost,
	(SELECT
		d.Name
	FROM TransportManage.dbo.DeliveryTask t
	INNER JOIN TransportManage.dbo.DriverCar dc
		ON dc.Id = t.DriverCarId
	INNER JOIN TransportManage.dbo.Employee d
		ON d.Id = dc.EmployeeId
	WHERE t.TaskNumber = ec.TaskNum)
	AS Driver
FROM TransportManage.[dbo].[CarExtraCost] ec
                                          WHERE TaskNum=@taskNum AND ec.CompanyId=@companyId";
        public const string GetAllComplete = @"DECLARE @CompleteTaskTable table (
LoadingTaskDetailId int,
UnloadingTaskDetailId int,
IsExistEvidence bit,
TaskId int,
TaskNumber nvarchar (max),
Name nvarchar (50),
Dump nvarchar (50),
PlateNumber nvarchar (50),
DriverPrice int,
LoadingTime datetime,
UnloadingTime datetime,
DumpPrice int,
WorkSitePrice int,
DriverName nvarchar (50),
ExtraCost int,
LoadingAddress nvarchar (max),
UnloadingAddress nvarchar (max)
);
INSERT INTO @CompleteTaskTable
	SELECT
 Top 800
		v.LoadingTaskDetailId,
		v.UnloadingTaskDetailId,
		v.IsExistEvidence,
		v.TaskId,
		v.TaskNumber,
		(SELECT
			ws.Name
		FROM TransportManage.dbo.WorkSite ws
		WHERE ws.Id = od.WorkSiteId)
		AS Name,
		(SELECT
			k.Name
		FROM TransportManage.dbo.KillMudstoneSite k
		WHERE k.Id = od.KillMudstoneSiteId)
		AS Dump,
		v.PlateNumber,
		od.DriverPrice,
		v.LoadingTime,
		v.UnloadingTime,
		od.KillSitePrice AS DumpPrice,
		od.WorkPrice AS WorkSitePrice,
		v.DriverName,
		(SELECT
			SUM(cc.Cost)
		FROM TransportManage.dbo.CarExtraCost cc
		WHERE cc.TaskNum = v.TaskNumber)
		AS ExtraCost,
		v.LoadingAddress,
		v.UnloadingAddress
	FROM TransportManage.dbo.DeliveryTaskView v
	INNER JOIN TransportManage.dbo.OrderDetail od
		ON od.Id = v.OrderDetailId
	INNER JOIN TransportManage..CheckedTask ct
		ON ct.TaskId = v.TaskId
	INNER JOIN TransportManage..Flow f
		ON f.FlowNumber=ct.FlowNumber
	WHERE v.TaskStatus = 2 AND v.CompanyId = @companyId AND f.Name = '消纳审核'
	ORDER BY v.CompleteTime DESC

SELECT
	*
FROM @CompleteTaskTable

SELECT
	tp.ImageUrl,
	TaskDetailId
FROM TransportManage.dbo.TaskPhoto tp
WHERE tp.TaskDetailId IN (SELECT
	ct.LoadingTaskDetailId
FROM @CompleteTaskTable ct)

SELECT
	tp.ImageUrl,
	TaskDetailId
FROM TransportManage.dbo.TaskPhoto tp
WHERE tp.TaskDetailId IN (SELECT
	ct.UnloadingTaskDetailId
FROM @CompleteTaskTable ct)
";
        public const string BossCheckTask = @"
if exists (SELECT
	*
FROM TransportManage.dbo.DeliveryTaskView t
WHERE t.TaskId = @taskId AND t.CompanyId=@companyId)
begin

UPDATE TransportManage.dbo.DeliveryTask
SET TaskStatus = 4 ,CheckTime=GETDATE() ,Operater=@EmployeeId
WHERE Id IN (@taskId) AND TaskStatus = 2;
SELECT
	'succeed'
end
else
begin

SELECT
	'defeated'
END";
        public const string AdminCheckTask = @"if exists (SELECT
	*
FROM TransportManage.dbo.DeliveryTaskView t
inner join TransportManage.dbo.OrderDetail od on od.Id=t.OrderDetailId
inner JOIN TransportManage.dbo.WorkSite w on w.Id=od.WorkSiteId
WHERE w.Id IN (SELECT
	wag.WorkSiteId
FROM TransportManage.dbo.WorkSiteAdminGroup wag
WHERE wag.EmployeeId = @EmployeeId UNION SELECT
	sgd.WorkSiteId
FROM TransportManage.dbo.Employee e
INNER JOIN TransportManage.dbo.EmployeeCorelation ec
	ON e.Id = ec.EmployeeId
INNER JOIN TransportManage.dbo.ShareGroupDetail sgd
	ON sgd.ShareDepartmentId = ec.DepartmentId
WHERE e.Id = @EmployeeId) AND t.TaskId = @TaskId AND t.CompanyId=@companyId)
begin

UPDATE TransportManage.dbo.DeliveryTask
SET TaskStatus = 4 ,CheckTime=GETDATE() Operater=@EmployeeId
WHERE Id IN (@taskId) AND TaskStatus = 2;
SELECT
	'succeed'
end
else
begin

SELECT
	'defeated'
END";
        public const string GetAuthLoginSql = @"SELECT
	e.Id,
	e.CompanyId,
	e.Tel,
	e.Name,
	App.dbo.Concatenate(d.Type, ';') AS AuthList
FROM TransportManage..Department d
INNER JOIN TransportManage..EmployeeCorelation ec
	ON ec.DepartmentId = d.Id
INNER JOIN TransportManage..Employee e
	ON e.Id = ec.EmployeeId
WHERE e.Name = @name AND e.Password = @PassWord AND d.Type LIKE '%权限'
GROUP BY	e.Id,
			e.CompanyId,
			e.Tel,
			e.Name";
        public const string CreateRemark = @"INSERT TransportManage.dbo.CarExtraCost (TaskNum,Date,Createdate,Remark,Cost,CompanyId) VALUES(@TaskNum,@Date,GETDATE(),@Remark,@Cost,@CompanyId)";
        public const string GetNumber = @"declare
@x1 int,
@y1 char(8)
select @x1= COUNT(CharNo) from FlowNo where CAST(DT as date)=CAST(GETDATE() as date)
if(@x1=0)
begin
select @y1=convert(char(8),getdate(),112)
insert FlowNo(CharNo,DT) values(
'ZT'+@y1+'0001'
,GETDATE())
end
else
begin
  insert FlowNo(CharNo,DT) values(
'ZT'+cast((select MAX(CAST( SUBSTRING(charno,3,12) as bigint))+1 from FlowNo where CAST(DT as date)=CAST(GETDATE() as date))as varchar(50))
,GETDATE())
end
select TOP(1)CharNo from FlowNo ORDER by Dt DESC";
        public const string StatisticsDetail = @"SELECT
	v.LoadingTaskId,
	v.UnloadingTaskId,
	v.TaskId,
	(SELECT
		SUM(CC.Cost)
	FROM TransportManage.dbo.CarExtraCost cc
	WHERE cc.TaskNum = v.TaskNumber)
	AS ExtraCost,
	v.TaskNumber,
	v.LoadingPos AS Name,
	v.UnloadingPos AS Dump,
	v.PlateNumber,
	od.DriverPrice,
	v.LoadingTime,
	(SELECT
		st.Name
	FROM TransportManage.dbo.SoilType st
	WHERE st.Id = od.SoilTypeId)
	AS SoilType,
	v.UnloadingTime AS UnloadTime,
	od.KillSitePrice AS DumpPrice,
	od.WorkPrice AS WorkSitePrice,
	v.DriverName AS Driver,
	v.LoadingAddress,
	v.UnloadingAddress,
CASE
		WHEN @type = 0 THEN od.DriverPrice
		WHEN @type = 1 THEN od.WorkPrice
		WHEN @type = 2 THEN od.KillSitePrice
	END
	AS UnitPrice
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = v.OrderDetailId
	inner join TransportManage..CheckedTask ct on ct.TaskId=v.TaskId
	INNER join TransportManage..Flow f on f.FlowNumber=ct.FlowNumber
WHERE ((@type = 0 AND v.DriverId = @id) OR
(@type = 1 AND od.WorkSiteId = @id) OR
(@type = 2 AND od.KillMudstoneSiteId = @id)
) AND v.CompleteTime BETWEEN @startTime AND @endTime  AND f.FlowNumber = @flowNumber AND v.CompanyId=@companyId
ORDER BY v.CompleteTime DESC;";
        public const string GetOrderDetail = @"SELECT
	o.Id,
	o.CreateTime,o.DueTime,o.Name,od.WorkPrice,
	CASE WHEN o.DueTime>GETDATE() THEN 0 ELSE 1 END AS isExpire ,
	od.Id AS orderDetailID,
	od.OrderDetailStatus,
	(SELECT ws.Name FROM TransportManage.dbo.WorkSite ws WHERE ws.Id=od.WorkSiteId) as WorkSiteName,
	(SELECT
		kms.Name
	FROM TransportManage.dbo.KillMudstoneSite kms
	WHERE kms.Id = od.KillMudstoneSiteId)
	AS Dump,
	od.KillSitePrice AS DumpPrice,
	(SELECT
		st.Name
	FROM TransportManage.dbo.SoilType st
	WHERE od.SoilTypeId = st.Id)
	AS soilType,
	od.DriverPrice,
	od.BossPrice
FROM TransportManage.dbo.OrderDetail od
INNER join TransportManage.dbo.[Order] o on od.OrderId=o.Id
WHERE o.Id=@orderId AND o.CompanyId=@companyId";
        public const string GetAdminOrder = @"SELECT
	o.Id,
	t.Name
	AS WorkSite,
	t.WorkPrice
	AS Price,
	o.CreateTime,
	o.DueTime,
	CASE
		WHEN o.OrderStatus = 1 THEN 0 ELSE 1
	END AS IsOpen,
	CASE
		WHEN o.DueTime > GETDATE() THEN 0 ELSE 1
	END AS IsExpire
FROM TransportManage.[dbo].[Order] o
OUTER APPLY (SELECT
TOP 1
	ws.Name,
	od.WorkPrice,od.WorkSiteId
FROM TransportManage.dbo.OrderDetail od
INNER JOIN TransportManage.dbo.WorkSite ws
	ON ws.Id = od.WorkSiteId
WHERE od.OrderId = o.Id
ORDER BY od.Id)
t
WHERE o.CompanyId = @companyId /*condition*/
ORDER BY o.CreateTime DESC";
        public const string GetDumpResult = @"SELECT
	kms.Id,
	kms.Name,
	SUM(t.KillSitePrice) AS Price,
	COUNT(t.Id) AS Count
FROM TransportManage.dbo.KillMudstoneSite kms
LEFT JOIN (SELECT
	dt.Id,
	od.KillMudstoneSiteId,
	od.KillSitePrice
FROM TransportManage.dbo.DeliveryTask dt
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = dt.OrderDetailId
INNER JOIN TransportManage.dbo.[Order] o
	ON o.Id = od.OrderId
INNER JOIN TransportManage..CheckedTask ct
	ON ct.TaskId = dt.Id
INNER JOIN TransportManage..Flow f
	ON f.FlowNumber=ct.FlowNumber
WHERE ct.FlowNumber = @flowNumber /*condition*/ AND dt.CompleteTime BETWEEN @startTime AND @endTime
GROUP BY	dt.Id,
			od.KillMudstoneSiteId,
			od.KillSitePrice)
t
	ON t.KillMudstoneSiteId = kms.Id
WHERE kms.Id IN @id  
GROUP BY	kms.Id,
			kms.Name";
        public const string GetDumpId = @"SELECT kms.Id
FROM TransportManage.dbo.KillMudstoneSite kms
WHERE kms.CompanyId=@companyId";
        public const string GetWorkSiteId = @"SELECT
	ws.Id
FROM TransportManage.dbo.WorkSite ws
WHERE ws.CompanyId = @companyId";
        public const string GetWorkSiteStatistics = @"SELECT
	ws.Id,
	ws.Name,
	SUM(t.WorkPrice) AS Price,
	SUM(t.BossPrice) AS BossPrice,
	SUM(t.KillSitePrice) AS KillPrice,
	COUNT(t.Id) AS TaskCount
FROM TransportManage.dbo.WorkSite ws
LEFT JOIN (SELECT
	dt.Id,
	od.WorkSiteId,
	od.WorkPrice,
	od.BossPrice,
	od.KillSitePrice
FROM TransportManage.dbo.[OrderDetail] od
INNER JOIN TransportManage.dbo.DeliveryTask dt
	ON dt.OrderDetailId = od.Id
INNER JOIN TransportManage.dbo.[Order] o
	ON o.Id = od.OrderId
INNER JOIN TransportManage..CheckedTask ct
	ON ct.TaskId = dt.Id
INNER JOIN TransportManage..Flow f
	ON f.FlowNumber = ct.FlowNumber
WHERE f.FlowNumber = @FlowNumber /*condition*/ AND dt.CompleteTime BETWEEN @startTime AND @endTime)
t
	ON t.WorkSiteId = ws.Id
WHERE ws.Id IN @id
GROUP BY	ws.Id,
	ws.Name";
        public const string GetDriverStatistics = @"SELECT
	d.Id,
	d.Name,
	SUM(t.DriverPrice) AS Price,
	COUNT(t.TaskId) AS Count
FROM TransportManage.dbo.Employee d
LEFT JOIN (SELECT
	dc.EmployeeId,
	od.DriverPrice,
	dt.Id AS TaskId
FROM TransportManage.dbo.DeliveryTask dt
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = dt.OrderDetailId
INNER JOIN TransportManage.dbo.[Order] o
	ON o.Id = od.OrderId
INNER JOIN TransportManage.dbo.DriverCar dc
	ON dc.Id = dt.DriverCarId
INNER JOIN TransportManage..CheckedTask ct
	ON ct.TaskId = dt.Id
INNER JOIN TransportManage..Flow f
	ON f.FlowNumber=ct.FlowNumber
WHERE  ct.FlowNumber= @FlowNumber /*condition*/ AND dt.CompleteTime BETWEEN @startTime AND @endTime)
t
	ON d.Id = t.EmployeeId
WHERE d.Id IN @id
	GROUP by d.Id,d.Name
	";
        public const string GetCompanyDriverId = @"SELECT DISTINCT
	e.Id
FROM [TransportManage].[dbo].[Employee] e
INNER JOIN TransportManage..EmployeeLevel el
	ON e.Id = el.EmployeeId
WHERE e.CompanyId = @companyId AND el.Name = '司机'";
        public const string GetKillSiteNameSql = @" 
 SELECT kms.Id
 FROM TransportManage.dbo.KillMudstoneSite kms
 WHERE kms.Name like '%'+ @Name +'%' AND kms.CompanyId=@companyId";
        public const string GetWorkSiteNameSql = @"SELECT ws.Id
 FROM TransportManage.dbo.WorkSite ws
 WHERE ws.Name like '%'+ @Name +'%' AND ws.CompanyId=@companyId";
        public const string GetDriverNameSql = @"SELECT Top 1 d.Id
 FROM TransportManage.dbo.Employee d
 INNER JOIN TransportManage..EmployeeLevel el ON d.Id=el.EmployeeId
 WHERE d.Name like '%'+ @Name +'%' and d.CompanyId=@companyId AND el.Name='司机'";


        public const string VerifyTaskstatus = @"SELECT t.TaskStatus
  FROM [TransportManage].[dbo].[DeliveryTask] t
  WHERE t.Id=@taskId";
        public const string VerifyDriver = @"	SELECT 'ok'
	  FROM [TransportManage].[dbo].[DriverCar] dc
	  inner JOIN TransportManage.dbo.DeliveryTask t on t.DriverCarId=dc.Id
	  WHERE dc.EmployeeId=@driverId AND t.Id=@taskId";
        public const string verifyTask = @"SELECT
	t.TaskStatus,
	o.DueTime
FROM [TransportManage].[dbo].[DeliveryTask] t
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = t.OrderDetailId
INNER JOIN TransportManage.dbo.[Order] o
	ON o.Id = od.OrderId
WHERE t.Id = @id";
        public const string UpdateUnloadingTaskStatus = @"UPDATE TransportManage.dbo.DeliveryTask SET TaskStatus=@status, CompleteTime=GETDATE() WHERE Id=@id";
        public const string UpdateLoadingTaskStatus = @"UPDATE TransportManage.dbo.DeliveryTask SET TaskStatus=@status WHERE Id=@id";
        public const string GetCarId = @" SELECT dc.CarId
  FROM [TransportManage].[dbo].[DriverCar] dc
  WHERE dc.EmployeeId in (@driverId)";
        public const string GetDriverCar = @" SELECT dc.Id
  FROM [TransportManage].[dbo].[DriverCar] dc
  WHERE dc.EmployeeId in (@driverId)";
        public const string GetEmployeesCar = @"SELECT count(*)
  FROM [TransportManage].[dbo].[DriverCar] dc
  WHERE dc.EmployeeId in @employeeId";
        public const string CreateOrderDetail = @"INSERT TransportManage.dbo.OrderDetail (KillMudstoneSiteId,WorkSiteId,SoilTypeId,CompanyId,BossPrice,WorkPrice,OrderId,DriverPrice,KillSitePrice,Operater,ExtraPrice) VALUES (@KillMudstoneSiteId,@WorkSiteId,@SoilTypeId,@CompanyId,@BossPrice,@WorkPrice,@OrderId,@DriverPrice,@DumpPrice,@Operater,@ExtraPrice)";
        public const string CreateOrder = @"INSERT INTO TransportManage.dbo.[Order](Name,CreateTime,DueTime,CompanyId,Operater) VALUES (@Name,@CreateTime,@DueTime,@CompanyId,@Operater);SELECT CAST(SCOPE_IDENTITY() as int);";
        public const string GetOrder = @"DECLARE @OrderInfo TABLE (
OrderId int,
CreateTime Datetime,
workSite nvarchar (50),
WorkPrice money,
BossPrice money,
WorkSiteId int,
DriverPrice int
);

INSERT INTO @OrderInfo
	SELECT
		o.Id AS OrderId,
		o.CreateTime,
		t.workSite,
		t.WorkPrice,
		t.BossPrice,
		t.WorkSiteId,
		t.DriverPrice
	FROM TransportManage.[dbo].[Order] o
	OUTER APPLY (SELECT TOP 1
		ws.Name AS workSite,
		od.WorkPrice,
		od.BossPrice,
		od.WorkSiteId,
		od.DriverPrice
	FROM TransportManage.dbo.OrderDetail od
	INNER JOIN TransportManage.dbo.WorkSite ws
		ON ws.Id = od.WorkSiteId
	WHERE od.OrderId = o.Id)
	t
	WHERE o.OrderStatus = 1 AND o.CompanyId = @companyId AND o.DueTime > GETDATE() /*condition*/

IF ((SELECT
	COUNT(*)
FROM TransportManage..EmployeeCorelation ec
INNER JOIN TransportManage..Department d
	ON d.Id = ec.DepartmentId
INNER JOIN TransportManage..ResourceAuthorization ra
	ON ra.DepartmentId = d.Id
WHERE ra.Type = '指派资源' AND ec.EmployeeId = @EmployeeId) > 0)

begin
SELECT
	*
FROM @OrderInfo o
WHERE o.WorkSiteId IN (SELECT
	ra.SiteId
FROM TransportManage..EmployeeCorelation ec
INNER JOIN TransportManage..Department d
	ON d.Id = ec.DepartmentId
INNER JOIN TransportManage..ResourceAuthorization ra
	ON ra.DepartmentId = d.Id
WHERE ra.Type = '指派资源' AND ec.EmployeeId = 2)
end
else
BEGIN
SELECT
	*
FROM @OrderInfo o
WHERE o.WorkSiteId NOT IN (SELECT
	ra.SiteId
FROM TransportManage..ResourceAuthorization ra
WHERE ra.Type = '指派资源')
end;

SELECT
	od.OrderId,
	od.Id AS OrderDetailId,
	st.Id AS SoilId,
	st.Name AS SoilName,
	kms.Id AS KillSiteId,
	kms.Name AS KillSiteName,
	od.DriverPrice
FROM TransportManage.dbo.OrderDetail od
INNER JOIN TransportManage.dbo.WorkSite ws
	ON ws.Id = od.WorkSiteId
INNER JOIN TransportManage.dbo.KillMudstoneSite kms
	ON kms.Id = od.KillMudstoneSiteId
INNER JOIN TransportManage.dbo.SoilType st
	ON st.Id = od.SoilTypeId
WHERE od.OrderId IN (SELECT
	oi.OrderId
FROM @OrderInfo oi)
AND od.OrderDetailStatus = 1";
        public const string GetBossWorkSite = @"SELECT ws.Id,ws.Name
  FROM TransportManage.[dbo].[WorkSite] ws 
  where ws.CompanyId=@id and ws.WorkSiteStatus=0";
        public const string GetSoilType = @"SELECT st.Id,st.Name
  FROM TransportManage.[dbo].[SoilType] st
  where st.CompanyId=@id AND TypeStatus=0";
        public const string GetKillSite = @"SELECT kms.Id,kms.Name
  FROM TransportManage.[dbo].[KillMudstoneSite] kms 
  where kms.CompanyId=@id and kms.KillMudstoneSiteStatus=0";
        public const string DelWorkAddress = @"UPDATE TransportManage.dbo.WorkSite SET WorkSiteStatus=1 WHERE Id=@id AND CompanyId=@companyId";
        public const string DelKillAddress = @"UPDATE TransportManage.dbo.KillMudstoneSite SET KillMudstoneSiteStatus=1 WHERE Id=@id AND CompanyId=@companyId";
        public const string WorkAddress = @"INSERT TransportManage.dbo.WorkSite (Name,CompanyId,WorkSiteStatus,Operater) OUTPUT inserted.Id  VALUES (@Name,@CompanyId,0,@Operater)";
        public const string KillAddress = @"INSERT TransportManage.dbo.KillMudstoneSite (Name,CompanyId,KillMudstoneSiteStatus,Operater) OUTPUT inserted.Id  VALUES (@Name,@CompanyId,0,@Operater)";
        public const string Cancel = @"UPDATE TransportManage.dbo.DeliveryTask SET TaskStatus=3 WHERE Id=@TaskId";
        public const string GetAddress = @"SELECT ws.Id,ws.Name,'工地' AS SiteType
  FROM TransportManage.[dbo].[WorkSite] ws
 WHERE ws.CompanyId =@companyId AND ws.WorkSiteStatus=0
 UNION SELECT kms.Id,kms.Name,'消纳场' AS SiteType
 FROM TransportManage.dbo.KillMudstoneSite kms
 WHERE kms.CompanyId=@companyId AND kms.KillMudstoneSiteStatus=0";
        public const string GetDriverFinishOrder = @"SELECT
t.TaskNumber,
	td.ImageUrl,
    td.TaskStep,
	d.Name AS DriverName,
	c.PlateNumber,
	t.OrderDetailId,
	CASE
		WHEN td.TaskStep = '装货' THEN (SELECT
			ws.Name
		FROM TransportManage.dbo.OrderDetail od
		INNER JOIN TransportManage.dbo.WorkSite ws
			ON ws.Id = od.WorkSiteId
		WHERE od.Id = t.OrderDetailId)
	ELSE (SELECT
		kms.Name
	FROM TransportManage.dbo.OrderDetail od
	INNER JOIN TransportManage.dbo.KillMudstoneSite kms
		ON kms.Id = od.KillMudstoneSiteId
	WHERE od.Id = t.OrderDetailId)
	END AS Pos,
	t.Id AS TaskId,
	td.CreateDate AS
	TaskDetailDate
FROM TransportManage.dbo.DeliveryTaskDetail td
INNER JOIN TransportManage.dbo.DeliveryTask t
	ON t.Id = td.DeliveryTaskId
INNER JOIN TransportManage.dbo.DriverCar dc
	ON dc.Id = t.DriverCarId
INNER JOIN TransportManage.dbo.Employee d
	ON d.Id = dc.EmployeeId
INNER JOIN TransportManage.dbo.Car c
	ON c.Id = dc.CarId
WHERE (t.TaskStatus = 2 or t.TaskStatus=4) AND d.Id=@DriverId
ORDER BY td.CreateDate DESC";
        public const string InsertTaskNext = @"insert TransportManage.dbo.DeliveryTaskDetail (lat,lng,DeliveryTaskId,CreateDate,TaskStep,Marker,TaskStepAddress,DdLocation) VALUES(@lat,@lng,@DeliveryTaskId,GETDATE(),@TaskStep,@Marker,@TaskStepAddress,@DdLocation);SELECT CAST(SCOPE_IDENTITY() as int);";
        public const string AddTask = @"insert TransportManage.dbo.DeliveryTask (TaskStatus,OrderDetailId,DriverCarId,CreateDate,CompanyId,TaskNumber,WorkSitePrice,DumpPrice,DriverPirce,ExtraPrice) OUTPUT inserted.Id VALUES(0,@OrderDetailId,@DriverCarId,GETDATE(),@CompanyId,@number,@WorkSitePrice,@DumpPrice,@DriverPirce,@ExtraPrice)";
        public const string GetDriverStatus = @"SELECT TOP 1 t.TaskStatus,dc.Id AS DriverCarId
  FROM TransportManage.[dbo].[DeliveryTask] t
  inner JOIN TransportManage.dbo.DriverCar dc  ON t.DriverCarId=dc.Id
  WHERE dc.EmployeeId=@driverId AND t.TaskStatus<2
  ORDER by t.CreateDate DESC";
        public const string GetRunTaskDetail = @"SELECT
	t.Id,
	td.CreateDate AS Time,
	tp.ImageUrl
FROM TransportManage.dbo.DeliveryTaskDetail td
INNER JOIN TransportManage.dbo.DeliveryTask t
ON t.Id = td.DeliveryTaskId
	inner join TransportManage.dbo.TaskPhoto tp on tp.TaskDetailId=td.Id
WHERE t.Id=@taskId";
        public const string GetRuningTask = @"SELECT
	v.TaskNumber,
	v.TaskId AS Id,
	v.TaskStatus,
	(SELECT
		ws.Name
	FROM TransportManage.dbo.WorkSite ws
	WHERE ws.Id = od.WorkSiteId)
	AS WorkSite,
	(SELECT
		kms.Name
	FROM TransportManage.dbo.KillMudstoneSite kms
	WHERE kms.Id = od.KillMudstoneSiteId)
	AS KillSite,
	od.KillSitePrice AS DumpPrice,
	(SELECT
		st.Name
	FROM TransportManage.dbo.SoilType st
	WHERE st.Id = od.SoilTypeId)
	AS SoilType,
	od.DriverPrice,
	v.LoadingAddress,
	v.UnloadingAddress
FROM TransportManage.dbo.DeliveryTaskView v
INNER JOIN TransportManage.dbo.OrderDetail od
	ON od.Id = v.OrderDetailId
WHERE v.DriverId = @driverID AND v.CompanyId=@companyId AND v.TaskStatus < 2 AND v.TaskStatus >= 0";
        public const string GetDrive = @"SELECT
	d.Name AS DriverName,
	(SELECT c.PlateNumber FROM TransportManage.dbo.Car c WHERE c.Id=dc.CarId) AS Cph,
	d.CompanyId,
	(SELECT
		COUNT(*)
	FROM TransportManage.dbo.DeliveryTask t
	WHERE DATEDIFF(MONTH, t.CompleteTime, GETDATE()) = 0 AND t.DriverCarId = dc.Id AND (t.TaskStatus = 2 OR t.TaskStatus=4))
	AS MounthCount,
	(SELECT
		COUNT(*)
	FROM TransportManage.dbo.DeliveryTask t
	WHERE DATEDIFF(DAY, t.CompleteTime, GETDATE()) = 0 AND t.DriverCarId = dc.Id AND (t.TaskStatus =2 OR t.TaskStatus=4))
	AS DayCount
FROM TransportManage.dbo.DriverCar dc
INNER JOIN TransportManage.dbo.Employee d
	ON d.Id = dc.EmployeeId
WHERE dc.EmployeeId = @driverId
 ";
        public const string GetOrderCount = @"DECLARE @templateTable table (
TaskId int,
OrderDetailId int,
OrderId int,
CompleteTime datetime,
TaskStatus int,
WorkSiteId int
);

INSERT @templateTable
	SELECT
		t.Id AS TaskId,
		t.OrderDetailId,
		o.Id AS orderId,
		t.CompleteTime,
		t.TaskStatus,
		od.WorkSiteId
	FROM TransportManage.dbo.DeliveryTask t
	INNER JOIN TransportManage.dbo.OrderDetail od
		ON od.Id = t.OrderDetailId
	INNER JOIN TransportManage.dbo.[Order] o
		ON o.Id = od.OrderId
where o.CompanyId=@companyId
SELECT
	e.Name ,
	(SELECT
		COUNT(*)
	FROM @templateTable t
	WHERE DATEDIFF(MONTH, t.CompleteTime, GETDATE()) = 0 AND t.WorkSiteId IN (@worksiteIdList) AND (t.TaskStatus = 2 OR t.TaskStatus = 4))
	AS MounthCount,
	(SELECT
		COUNT(*)
	FROM  @templateTable t
	WHERE DATEDIFF(DAY, t.CompleteTime, GETDATE()) = 0 AND t.WorkSiteId IN  (@worksiteIdList)  AND (t.TaskStatus = 2 OR t.TaskStatus = 4))
	AS DayCount,
	(SELECT
		COUNT(*)
	FROM  @templateTable t
	WHERE t.TaskStatus < 2 AND t.TaskStatus >= 0 AND t.WorkSiteId IN  (@worksiteIdList))
	AS TaskCount,
    c.CreateTime,
c.DueTime
FROM TransportManage.dbo.Employee e
inner join TransportManage..Company c on c.Id=e.CompanyId
WHERE e.Id=@EmployeeId";
        public const string GetFinishOrder = @"SELECT
    t.TaskNumber,
	td.ImageUrl,
	d.Name AS DriverName,
	c.PlateNumber,
	t.OrderDetailId,
	td.TaskStep,
	CASE
		WHEN td.TaskStep = '装货' THEN (SELECT
			ws.Name
		FROM TransportManage.dbo.OrderDetail od
		INNER JOIN TransportManage.dbo.WorkSite ws
			ON ws.Id = od.WorkSiteId
		WHERE od.Id = t.OrderDetailId)
	ELSE (SELECT
		kms.Name
	FROM TransportManage.dbo.OrderDetail od
	INNER JOIN TransportManage.dbo.KillMudstoneSite kms
		ON kms.Id = od.KillMudstoneSiteId
	WHERE od.Id = t.OrderDetailId)
	END AS Pos,
	t.Id AS TaskId,
	td.CreateDate AS
	TaskDetailDate
FROM TransportManage.dbo.DeliveryTaskDetail td
INNER JOIN TransportManage.dbo.DeliveryTask t
	ON t.Id = td.DeliveryTaskId
INNER JOIN TransportManage.dbo.DriverCar dc
	ON dc.Id = t.DriverCarId
INNER JOIN TransportManage.dbo.Employee d
	ON d.Id = dc.EmployeeId
INNER JOIN TransportManage.dbo.Car c
	ON c.Id = dc.CarId
WHERE (t.TaskStatus = 2 or t.TaskStatus=4)AND t.CompanyId=@CompanyId
ORDER BY td.CreateDate DESC";
        public const string InsertCarInfo = @"insert TransportManage.dbo.Car (PlateNumber,CompanyId) VALUES (@PlateNumber,@CompanyId);SELECT CAST(SCOPE_IDENTITY() as int);";
        public const string InsertDriverInfo = @"DECLARE @temp table (
CompanyDueTime datetime
)
INSERT @temp
	SELECT
		c.DueTime
	FROM TransportManage..Company c
	WHERE c.Id = @companyId
IF (SELECT
	t.CompanyDueTime
FROM @temp t) > GETDATE ()
begin
INSERT TransportManage..Employee (Name, Tel, Password, CompanyId, CreateTime, DueTime)
	VALUES (@Name, @Tel, @Password, @CompanyId, (SELECT
		t.CompanyDueTime
	FROM @temp t)
	, (SELECT
		t.CompanyDueTime
	FROM @temp t)
	+ 30);SELECT CAST(SCOPE_IDENTITY() as int);
end
else
begin
INSERT TransportManage..Employee (Name, Tel, Password, CompanyId, CreateTime, DueTime)
	VALUES (@Name, @Tel, @Password, @CompanyId, GETDATE(), GETDATE() + 30);SELECT CAST(SCOPE_IDENTITY() as int);
end";
        public const string InsertClientCode = @"INSERT INTO TransportManage.dbo.Code (Code,ClientId,Expiration) VALUES(@Code,@ClientId,@Expiration)";
        public const string InsertDriverCode = @"INSERT INTO TransportManage.dbo.Code (Code,DriverId,Expiration) VALUES(@Code,@DriverId,@Expiration)";
        public const string Code = @"SELECT *
  FROM [TransportManage].[dbo].[Code] c
  WHERE c.Code=@Code";
        public const string RegisterClient = @"UPDATE TransportManage.dbo.Client SET OpenId = @OpenId WHERE Id = ClientId";
        public const string RegisterDriver = @"UPDATE TransportManage.dbo.Employee SET OpenId = @OpenId WHERE Id = DriverId";
        public const string InsertClient = @"INSERT INTO TransportManage.dbo.Client(Name,Tel,CompanyId) VALUES(@Name,@Tel,@CompanyId)";
    }
}