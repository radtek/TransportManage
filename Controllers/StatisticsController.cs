using AppHelpers;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Controllers;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage
{

    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/Statistics")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class StatisticsController : ApiController
    {

        /// <summary>
        /// 获取司机统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="driverName">司机名称可空类型</param>
        ///<param name="token">token</param>
        [HttpGet, Route("GetDriver")]
        public object GetDriver(string token, int taskStatus, string driverName, DateTime? startTime, DateTime? endTime)
        {

            if (startTime == null)
            {
                return ApiResult.Create(false, "起始时间不能为空");
            }
            if (endTime == null)
            {
                return ApiResult.Create(false, "结束时间不能为空");
            }
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var employeeId = Convert.ToInt32(Request.Properties["Id"]);
                var companyId = Request.Properties["CompanyId"];
                taskStatus = taskStatus == 2 ? c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { }) : c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });

                if (userInfo.IsBoss)
                {
                    var driverId = new List<int>();
                    if (string.IsNullOrWhiteSpace(driverName))
                    {
                        driverId = c.Query<int>(Sql.GetCompanyDriverId, new { companyId }).AsList();
                    }
                    else
                    {//搜索单个司机的统计
                        var selectResult = GetId(driverName, Convert.ToInt32(companyId), NameTypeEnum.DriverName);
                        if (selectResult.Status == false)
                        {
                            return ApiResult.Create(false, "关于此名称没有相应的数据");
                        }
                        driverId = selectResult.Result;
                        var DriverInfo = c.Query<DriverResult>(Sql.GetDriverStatistics, new { id = driverId, startTime, endTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus }).AsList();
                        return ApiResult.Create(true, new
                        {
                            AllPrice = DriverInfo.Sum(d => d.Price),
                            TaskCount = DriverInfo.Sum(d => d.Count),
                            StartTime = startTime,
                            EndTime = endTime,
                            Detail = DriverInfo
                        });
                    }

                    var driverResult = c.Query<DriverResult>(Sql.GetDriverStatistics, new { id = driverId, startTime, endTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus }).AsList();

                    return ApiResult.Create(true, new
                    {
                        AllPrice = driverResult.Sum(d => d.Price),
                        TaskCount = driverResult.Sum(d => d.Count),
                        StartTime = startTime,
                        EndTime = endTime,
                        Detail = driverResult
                    });
                }
                else if (userInfo.IsAdmin && !userInfo.IsBoss)
                {
                    //该工管工地上所有司机
                    var adminDriverId = c.Query<int>(Sql.GetAdminDriver, new { employeeId, companyId }).ToList();
                    if (string.IsNullOrWhiteSpace(driverName))
                    {

                        var DriverInfo = c.Query<DriverResult>(Sql.GetDriverStatistics.Replace("condition", Sql.GetAdminStaticCondition), new { id = adminDriverId, startTime, endTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNuber = taskStatus, Employee = Convert.ToInt32(Request.Properties["Id"]) }).AsList();
                        return ApiResult.Create(true, new
                        {
                            AllPrice = DriverInfo.Sum(d => d.Price),
                            TaskCount = DriverInfo.Sum(d => d.Count),
                            StartTime = startTime,
                            EndTime = endTime,
                            Detail = DriverInfo
                        });
                    }
                    else
                    {//根据司机名称查找
                        var selectResult = GetId(driverName, 2, NameTypeEnum.DriverName);
                        if (selectResult.Status == false)
                        {
                            return ApiResult.Create(false, "关于此名称没有相应的数据");
                        }
                        var cr = (List<int>)selectResult.Result;
                        var id = adminDriverId.Intersect(cr);
                        var DriverInfo = c.Query<DriverResult>(Sql.GetDriverStatistics.Replace("condition", Sql.GetAdminStaticCondition), new { id, startTime, endTime = endTime.Value.Date.AddDays(1).AddSeconds(-1), flowNumber = taskStatus, Employee = Convert.ToInt32(Request.Properties["Id"]) }).AsList();

                        return ApiResult.Create(true, new
                        {
                            AllPrice = DriverInfo.Sum(d => d.Price),
                            TaskCount = DriverInfo.Sum(d => d.Count),
                            StartTime = startTime,
                            EndTime = endTime,
                            Detail = DriverInfo
                        });
                    }
                }
                else
                {
                    return ApiResult.Create(false, "没有相应的权限");
                }

            }
        }

        /// <summary>
        /// 获取工地统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="workSite">工地名称可空类型</param>
        /// <param name="token">token</param>
        ///<param name="taskStatus">0为运输完成</param>
        /// <returns>获取工地统计</returns>
        [HttpGet, Route("GetWorkSite")]
        public object GetWorkSite(int taskStatus, DateTime? startTime, DateTime? endTime, string workSite, string token)
        {
            if (startTime == null)
            {
                return ApiResult.Create(false, "起始时间不能为空");
            }
            if (endTime == null)
            {
                return ApiResult.Create(false, "结束时间不能为空");
            }
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                var employeeId = Request.Properties["Id"];
                taskStatus = taskStatus == 2 ? c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { }) : c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });


                if (userInfo.IsBoss)
                {
                    var id = new List<int>();

                    if (string.IsNullOrWhiteSpace(workSite))
                    {
                        id = c.Query<int>(Sql.GetWorkSiteId, new { companyId }).ToList();
                    }
                    else
                    {
                        var selectResult = GetId(workSite, companyId, NameTypeEnum.WorkSite);
                        if (selectResult.Status == false)
                        {
                            ApiResult.Create(false, "关于此名称没有相应的数据");
                        }
                        id = selectResult.Result;
                        var WorkSiteInfo = c.Query<WorkSiteResult>(Sql.GetWorkSiteStatistics, new { id, startTime, EndTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus }).AsList();
                        return ApiResult.Create(true, new
                        {
                            AllPrice = WorkSiteInfo.Sum(d => d.Price),
                            AllBossPrice = WorkSiteInfo.Sum(d => d.BossPrice),
                            AllKillPrice = WorkSiteInfo.Sum(d => d.KillPrice),
                            StartTime = startTime,
                            EndTime = endTime,
                            Detail = WorkSiteInfo
                        });

                    }
                    var workSiteResult = c.Query<WorkSiteResult>(Sql.GetWorkSiteStatistics, new { id, startTime, EndTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus }).AsList();

                    return ApiResult.Create(true, new
                    {
                        AllPrice = workSiteResult.Sum(d => d.Price),
                        AllBossPrice = workSiteResult.Sum(d => d.BossPrice),
                        AllKillPrice = workSiteResult.Sum(d => d.KillPrice),
                        StartTime = startTime,
                        EndTime = endTime,
                        Detail = workSiteResult
                    });
                }
                else if (userInfo.IsAdmin && !userInfo.IsBoss)
                {
                    var adminworksiteId = c.Query<AdminController.DataList>(Sql.GetAdminWorksite, new { employeeId, id = companyId }).AsList();

                    if (string.IsNullOrWhiteSpace(workSite))
                    {
                        var id = adminworksiteId.ConvertAll(d => d.Id);
                        var workSiteResult = c.Query<WorkSiteResult>(Sql.GetWorkSiteStatistics, new { id, startTime, EndTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus }).AsList();

                        return ApiResult.Create(true, new
                        {
                            AllPrice = workSiteResult.Sum(d => d.Price),
                            AllBossPrice = workSiteResult.Sum(d => d.BossPrice),
                            AllKillPrice = workSiteResult.Sum(d => d.KillPrice),
                            StartTime = startTime,
                            EndTime = endTime,
                            Detail = workSiteResult
                        });
                    }
                    else
                    {
                        var selectResult = GetId(workSite, companyId, NameTypeEnum.WorkSite);
                        if (selectResult.Status == false)
                        {
                            return ApiResult.Create(false, "关于此名称没有相应的数据");
                        }
                        var cr = (List<int>)selectResult.Result;
                        var id = adminworksiteId.ConvertAll(d => d.Id).Intersect(cr);
                        if (id.Count() != 0)
                        {
                            var WorkSiteInfo = c.Query<WorkSiteResult>(Sql.GetWorkSiteStatistics, new { id, startTime, EndTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus }).AsList();


                            return ApiResult.Create(true, new
                            {
                                AllPrice = WorkSiteInfo.Sum(d => d.Price),
                                AllBossPrice = WorkSiteInfo.Sum(d => d.BossPrice),
                                AllKillPrice = WorkSiteInfo.Sum(d => d.KillPrice),
                                StartTime = startTime,
                                EndTime = endTime,
                                Detail = WorkSiteInfo
                            });
                        }
                        else
                        {
                            return ApiResult.Create(false, "该工管没有对应工地权限");
                        }
                    }
                }
                else
                {
                    return ApiResult.Create(false, "没有相应的权限");
                }
            }
        }

        /// <summary>
        /// 获取消纳场统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="killSite">消纳场可空类型</param>
        /// <param name="token">token</param>
        /// <returns>消纳场统计</returns> [HttpGet, Route("GetKillSite")]
        [HttpGet, Route("GetKillSite")]
        public object GetKillSite(int taskStatus, DateTime? startTime, DateTime? endTime, string killSite, string token)
        {
            if (startTime == null)
            {
                return ApiResult.Create(false, "起始时间不能为空");
            }
            if (endTime == null)
            {
                return ApiResult.Create(false, "结束时间不能为空");
            }
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                var employeeId = Convert.ToInt32(Request.Properties["Id"]);
                taskStatus = taskStatus == 2 ? c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { }) : c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });

                if (userInfo.IsBoss)
                {
                    var id = new List<int>();
                    if (string.IsNullOrWhiteSpace(killSite))
                    {
                        id = c.Query<int>(Sql.GetDumpId, new { companyId }).ToList();
                    }
                    else
                    {
                        var selectResult = GetId(killSite, companyId, NameTypeEnum.KillSile);
                        if (selectResult.Status == false)
                        {
                            return ApiResult.Create(false, "关于此名称没有相应的数据");
                        }
                        id = selectResult.Result;
                        var dumpInfo = c.Query<DumpResult>(Sql.GetDumpResult, new { id, startTime, EndTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus, employeeId }).AsList();
                        return ApiResult.Create(true, new
                        {
                            AllPrice = dumpInfo.Sum(d => d.Price),
                            AllCount = dumpInfo.Sum(d => d.Count),
                            StartTime = startTime,
                            EndTime = endTime,
                            Detail = dumpInfo
                        });
                    }
                    var dumpResult = c.Query<DumpResult>(Sql.GetDumpResult, new { id, startTime, EndTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus, employeeId }).AsList();

                    return ApiResult.Create(true, new
                    {
                        AllPrice = dumpResult.Sum(d => d.Price),
                        AllCount = dumpResult.Sum(d => d.Count),
                        StartTime = startTime,
                        EndTime = endTime,
                        Detail = dumpResult
                    });
                }
                else if (userInfo.IsAdmin && !userInfo.IsBoss)
                {
                    var adminDumpId = c.Query<int>(Sql.GetAdminDump, new { employeeId, companyId }).AsList();
                    if (string.IsNullOrWhiteSpace(killSite))
                    {
                        var dumpResult = c.Query<DumpResult>(Sql.GetDumpResult, new { id = adminDumpId, startTime, EndTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNuber = taskStatus }).AsList();

                        return ApiResult.Create(true, new
                        {
                            AllPrice = dumpResult.Sum(d => d.Price),
                            AllCount = dumpResult.Sum(d => d.Count),
                            StartTime = startTime,
                            EndTime = endTime,
                            Detail = dumpResult
                        });
                    }
                    else
                    {
                        var selectResult = GetId(killSite, 2, NameTypeEnum.KillSile);
                        if (selectResult.Status == false)
                        {
                            return ApiResult.Create(false, "关于此名称没有相应的数据");
                        }
                        var cr = (List<int>)selectResult.Result;
                        var id = adminDumpId.Intersect(cr);
                        if (id.Count() != 0)
                        {
                            var dumpInfo = c.Query<DumpResult>(Sql.GetDumpResult, new { id, startTime, EndTime = endTime.Value.AddDays(1).AddSeconds(-1), flowNumber = taskStatus, EmployeeId = Convert.ToInt32(Request.Properties["Id"]) }).AsList();
                            return ApiResult.Create(true, new
                            {
                                AllPrice = dumpInfo.Sum(d => d.Price),
                                AllCount = dumpInfo.Sum(d => d.Count),
                                StartTime = startTime,
                                EndTime = endTime,
                                Detail = dumpInfo
                            });
                        }
                        else
                        {
                            return ApiResult.Create(false, "该工管没有对应工地权限");
                        }
                    }
                }
                else
                {
                    return ApiResult.Create(false, "没有相应的权限");
                }
            }
        }



        /// <summary>
        /// 获取统计第二层信息
        /// </summary>
        /// <param name="taskStatus"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="detailType"></param>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetOrderGroupDetail")]
        public object GetOrderGroupDetail(int taskStatus, DateTime? startTime, DateTime? endTime, DetailType detailType, int id, string token)
        {
            var data = GetDetail(taskStatus, startTime, endTime, detailType, id, token).Result;
            var result = from i in data
                         group i by new { i.PlateNumber, i.SoilType, i.UnitPrice } into t
                         select new
                         {
                             PlateNumber = t.Key.PlateNumber,
                             SoilType = t.Key.SoilType,
                             UnitPrice = t.Key.UnitPrice,
                             Count = t.Count(),
                             Detail = t
                         };
            return result;
        }



        public enum OrderResultType
        {
            工地, 消纳
        }
        [HttpGet, Route("GetOrderMonthGroupDetail")]
        public object GetOrderMonthGroupDetail(OrderResultType type, DateTime? startTime, DateTime? endTime, int companyId, int monthOrDay, int workSiteId)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                string sql = null;
                if (monthOrDay == 0)
                {//返回月份分组数据
                    sql = Sql.OrderMonthResultSql;
                }
                else if (monthOrDay == 1)
                {//返回日期分组数据
                    sql = Sql.OrderDayResultSql;
                }
                return c.Select<OrderResultData>(sql, new { type, companyId, startTime, endTime, workSiteId });

            }
        }




        /// <summary>
        /// 获取司机工地消纳场明细
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="detailType">明细类别</param>
        /// <param name="id">对应类别ID</param>
        /// <returns></returns>
        [HttpGet, Route("GetDetail")]
        public ApiResult<List<StatisticsDetail>> GetDetail(int taskStatus, DateTime? startTime, DateTime? endTime, DetailType detailType, int id, string token)
        {

            if (startTime == null)
            {
                throw new ArgumentNullException("开始时间不能为空");

            }
            if (endTime == null)
            {
                throw new ArgumentNullException("结束时间不能为空");

            }
            if (id == 0)
            {
                throw new ArgumentNullException("查询明细ID不能为空");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                taskStatus = taskStatus == 2 ? c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { }) : c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });

                //taskStatus = taskStatus == 2 ? c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { }) : c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });
                var r = c.Query<StatisticsDetail>(Sql.StatisticsDetail, new { startTime, endTime = endTime.Value.Date.AddDays(1).AddSeconds(-1), type = detailType, id, flowNumber = taskStatus, companyId = Request.Properties["CompanyId"] }).AsList();

                var loadingTaskDetailIds = r.ConvertAll(d => d.LoadingTaskId);
                using (var cmd = c.SetupCommand(Sql.GetTaskDetailPhoto, new { taskDetailId = loadingTaskDetailIds }))
                using (var imgReader = cmd.ExecuteReader())
                {
                    imgReader.JoinEntities<StatisticsDetail, ImageData, int>(r, d => d.LoadingImages, m => m.LoadingTaskId, "TaskDetailId");
                }
                var unloadingTaskDetailIds = r.ConvertAll(i => i.UnloadingTaskId);
                using (var cmd = c.SetupCommand(Sql.GetTaskDetailPhoto, new { taskDetailId = unloadingTaskDetailIds }))
                using (var imgReader = cmd.ExecuteReader())
                {
                    imgReader.JoinEntities<StatisticsDetail, ImageData, int>(r, d => d.UnloadingImages, m => m.UnloadingTaskId, "TaskDetailId");
                }

                foreach (var item in r)
                {
                    if (item.ExtraCost != 0)
                    {
                        item.IsExists = true;
                    }
                }
                return ApiResult.Create(true, r, "查询类型为" + detailType);
            }

        }

        /// <summary>
        /// 快速查询详细信息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("GetShortcutDetail")]
        public object GetShortcutDetail(ShortcutData data, string token)
        {
            if (data == null)
            {
                return ApiResult.Create(false, "数据为空");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                data.TaskStatus = data.TaskStatus == 2 ? c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { }) : c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });
                if ((data.DriverName ?? data.Dump ?? data.PlateNumber ?? data.Worksite) == null && (data.StartTime ?? data.EndTime) == null && data.TaskStatus == 0)
                {
                    throw new ArgumentNullException("参数不能全为空");
                }

                data.Worksite = "%" + data.Worksite;
                data.Dump = "%" + data.Dump;
                data.DriverName = "%" + data.DriverName;
                data.PlateNumber = "%" + data.PlateNumber;
                var ss = data.StartTime ?? new DateTime(2000, 1, 1);
                var ee = (data.EndTime ?? System.DateTime.Now).Date.AddDays(1).AddSeconds(-1);
                var r = c.Select<StatisticsController.StatisticsDetail>(Sql.GetShortcutDetailSql, new { StartTime = data.StartTime ?? Convert.ToDateTime("2000/1/1"), data.PlateNumber, EndTime = (data.EndTime ?? System.DateTime.Now).Date.AddDays(1).AddSeconds(-1), data.DriverName, data.Dump, data.Worksite, flowNumber = data.TaskStatus, CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]) });

                //MethodHelp.GetCompleteTaskDetail(r, 100, 100, c);
                var temp = r.Where(d =>
                {
                    return d.LoadingTaskId != 0;
                });
                var loadingTaskDetailIds = temp.ConvertAll(d => d.LoadingTaskId);
                using (var cmd = c.SetupCommand(Sql.GetTaskDetailPhoto, new { taskDetailId = loadingTaskDetailIds }))
                using (var imgReader = cmd.ExecuteReader())
                {
                    imgReader.JoinEntities(temp, d => d.LoadingImages, m => m.LoadingTaskId, "TaskDetailId");
                }
                var unloadingTaskDetailIds = temp.ConvertAll(i => i.UnloadingTaskId);
                using (var cmd = c.SetupCommand(Sql.GetTaskDetailPhoto, new { taskDetailId = unloadingTaskDetailIds }))
                using (var imgReader = cmd.ExecuteReader())
                {
                    imgReader.JoinEntities(temp, d => d.UnloadingImages, m => m.UnloadingTaskId, "TaskDetailId");
                }

                foreach (var item in r)
                {
                    if (item.ExtraCost != 0)
                    {
                        item.IsExists = true;
                    }
                }
                return ApiResult.Create(true, r);
            }


        }


        /// <summary>
        /// 司机完成运输单统计
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("GetDriverCompleteTask")]
        public object GetDriverCompleteTask(string token, DateTime? startTime = null, DateTime? endTime = null)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {

                string sql;
                var maxNumber = c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });
                var DumpCheckedNumber = c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { });

                if ((startTime ?? endTime) == null)
                {
                    sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", " AND v.DriverId = @Id AND v.LoadingTime is NULL AND (f.Name = '完成运输' OR f.Name = '完成审核') ");
                }
                else if (endTime != null & startTime != null)
                {
                    sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND v.DriverId = @Id AND v.LoadingTime >=@StartTime AND  v.UnloadingTime<=@EndTime  OR (v.DriverId=@Id AND v.LoadingTime is NULL AND v.LoadingTime is NULL AND (f.Name = '完成运输' OR f.Name = '完成审核'))");
                }
                else if (startTime != null)
                {
                    sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND v.DriverId = @Id  AND v.LoadingTime >=@StartTime  OR (v.DriverId=@Id AND v.LoadingTime is NULL AND v.LoadingTime is NULL AND (f.Name = '完成运输' OR f.Name = '完成审核'))");
                }
                else if (endTime != null)
                {
                    sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND v.DriverId = @Id AND  v.UnloadingTime<=@EndTime  OR (v.DriverId=@Id AND v.LoadingTime is NULL AND v.LoadingTime is NULL AND (f.Name = '完成运输' OR f.Name = '完成审核'))");
                }
                else
                {
                    throw new ArgumentNullException("时间参数异常");
                }

                var employeeId = Convert.ToInt32(Request.Properties["Id"]);
                var t = GetCompleteDetailMethord(startTime, endTime, c, maxNumber, DumpCheckedNumber, sql, employeeId: employeeId);
                return ApiResult.Create(true, new
                {
                    Price = t.Sum(d => d.DriverPrice + d.DriverPrice2),
                    ExtraCost = t.Sum(d => d.ExtraCost + d.ExtraCost2),
                    ElsePrice = t.Sum(d => d.Price),
                    Data = t
                });

            }

        }


        /// <summary>
        /// 获取司机完成运输单的详情
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("GetCompleteTaskDetail")]
        public object GetCompleteTaskDetail(List<int> data, string token)
        {
            var userInfo = new UserInfoData(Request);
            var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
            var employeeId = Convert.ToInt32(Request.Properties["Id"]);

            if (data == null)
            {
                throw new ArgumentNullException("参数taskId不能为空");
            }

            var r = new OrderController().GetCompleteTaskCarryTime(userInfo, companyId, employeeId, Convert.ToDateTime("2000/1/1"), DateTime.Now.Date, taskId: data);
            return ApiResult.Create(true, r.Data, r.Count.ToString());

        }

        /// <summary>
        /// 老板获取司机所有完成的订单
        /// </summary>
        /// <param name="token"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [HttpGet, Route("GetBossCompleteTask")]
        public object GetBossCompleteTask(string token, DateTime? startTime = null, DateTime? endTime = null)
        {
            var userInfo = new UserInfoData(Request);
            if (userInfo.IsAdmin || userInfo.IsBoss)
            {
                using (var c = AppDb.CreateConnection(AppDb.TransportManager))
                {
                    var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                    var employeeId = Convert.ToInt32(Request.Properties["Id"]);
                    var maxNumber = c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });
                    var DumpCheckedNumber = c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { });

                    string sql;
                    if ((startTime ?? endTime) == null)
                    {
                        if (userInfo.IsBoss)
                        {

                            sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND v.CompanyId=@companyId AND v.LoadingTime is NULL AND (f.Name='消纳审核' or f.Name='完成审核'))");
                        }
                        else
                        {
                            sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND w.Id IN (@workSiteId) AND v.CompanyId=@companyId AND v.LoadingTime is NULL AND (f.Name='消纳审核' or f.Name='完成审核'))");
                        }
                    }
                    else if (endTime != null & startTime != null)
                    {
                        if (userInfo.IsBoss)
                        {

                            sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND v.CompanyId = @companyId AND v.LoadingTime >=@StartTime AND  v.UnloadingTime<=@EndTime  OR (v.CompanyId=@CompanyId  AND v.LoadingTime is NULL AND (f.Name='消纳审核' or f.Name='完成审核'))");
                        }
                        else
                        {
                            sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND w.Id IN (@workSiteId) AND v.CompanyId = @companyId AND v.LoadingTime >=@StartTime AND  v.UnloadingTime<=@EndTime  OR (v.CompanyId=@CompanyId  AND v.LoadingTime is NULL AND (f.Name='消纳审核' or f.Name='完成审核'))");
                        }
                    }
                    else if (startTime != null)
                    {
                        if (userInfo.IsBoss)
                        {
                            sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND v.CompanyId = @CompanyId  AND v.LoadingTime >=@StartTime  OR (v.CompanyId=@CompanyId AND v.LoadingTime is NULL AND (f.Name='消纳审核' or f.Name='完成审核'))");

                        }
                        else
                        {
                            sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND w.Id IN (@workSiteId) AND v.CompanyId = @CompanyId  AND v.LoadingTime >=@StartTime  OR (v.CompanyId=@CompanyId AND v.LoadingTime is NULL AND (f.Name='消纳审核' or f.Name='完成审核'))");
                        }
                    }
                    else if (endTime != null)
                    {
                        if (userInfo.IsBoss)
                        {

                            sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND v.CompanyId = @CompanyId AND  v.UnloadingTime<=@EndTime  OR (v.CompanyId=@CompanyId AND v.LoadingTime is NULL  AND (f.Name='消纳审核' or f.Name='完成审核'))");
                        }
                        else
                        {
                            sql = Sql.GetCompleteTaskDetailSql.Replace("/* condition */", "AND w.Id IN (@workSiteId) AND v.CompanyId = @CompanyId AND  v.UnloadingTime<=@EndTime  OR (v.CompanyId=@CompanyId AND v.LoadingTime is NULL  AND (f.Name='消纳审核' or f.Name='完成审核'))");
                        }
                    }
                    else
                    {
                        throw new ArgumentNullException("时间参数异常");
                    }
                    var t = GetCompleteDetailMethord(startTime, endTime, c, maxNumber, DumpCheckedNumber, sql, employeeId: employeeId, companyId: companyId);
                    return t;
                }
            }
            else
            {
                return ApiResult.Create(false, "没有权限调用此接口");
            }
        }

        /// <summary>
        /// 获取老板完成任务详情
        /// </summary>
        /// <param name="data"></param>
        /// <param name="taskStatus">2为完成，4为审核</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("GetBossCompleteTaskDetail")]
        public object GetBossCompleteTaskDetail(List<int> data, string token, int taskStatus = 0)
        {

            var userInfo = new UserInfoData(Request);
            var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
            var employeeId = Convert.ToInt32(Request.Properties["Id"]);

            if (data == null)
            {
                throw new ArgumentNullException("参数taskId不能为空");
            }
            var r = new OrderController().GetCompleteTaskCarryTime(userInfo, companyId, employeeId, Convert.ToDateTime("2000/1/1"), DateTime.Now.Date, taskId: data);


            if (taskStatus == 2)
            {
                //TODO 更正司机价格为应付司机价格，
                //获取未审核的订单
                var t = r.Data.Where(d => d.FlowName == "消纳审核");
                var DriverPrice = t.Sum(d => d.DriverPrice);
                var ExtraPrice = t.Sum(d => d.ExtraCost);
                return ApiResult.Create(true, new
                {
                    DumpPrice = t.Sum(d => d.DumpPrice),
                    WorkPrice = t.Sum(d => d.WorkPrice),
                    DriverPrice = DriverPrice - ExtraPrice,
                    Result = t
                }, r.Count.ToString());

            }
            else if (taskStatus == 4)
            {
                //获取已审核的订单
                var t = r.Data.Where(d => d.FlowName == "完成审核");
                return ApiResult.Create(true, new
                {
                    DumpPrice = t.Sum(d => d.DumpPrice),
                    WorkPrice = t.Sum(d => d.WorkPrice),
                    DriverPrice = t.Sum(d => d.DriverPrice),
                    Result = t
                }, r.Count.ToString());
            }
            else
            {//获取所有订单
                return ApiResult.Create(true, new
                {
                    DumpPrice = r.Data.Sum(d => d.DumpPrice),
                    WorkPrice = r.Data.Sum(d => d.WorkPrice),
                    DriverPrice = r.Data.Sum(d => d.DriverPrice),
                    Result = r.Data
                }, r.Count.ToString());
            }

        }




        /// <summary>
        /// 司机、老板或工地管理员获取完成任务详细信息
        /// </summary>
        /// <param name="startTime">任务开始时间</param>
        /// <param name="endTime">任务结束时间</param>
        /// <param name="c"></param>
        /// <param name="sql">不同角色的筛选条件的Sql</param>
        /// <param name="employeeId">用户Id</param>
        /// <param name="companyId">公司Id</param>
        /// <returns></returns>
        protected List<DriverCompleteTask> GetCompleteDetailMethord(DateTime? startTime, DateTime? endTime, System.Data.SqlClient.SqlConnection c, int maxNumber, int checkedDumpNumber, string sql, int employeeId = 0, int companyId = 0)
        {
            var adminworksiteId = c.Query<AdminController.DataList>(Sql.GetAdminWorksite, new { employeeId, id = companyId }).AsList();

            using (var db = c.SetupCommand(sql, new { maxNumber, DumpCheckedNumber = checkedDumpNumber, Id = employeeId, CompanyId = companyId, startTime, endTime, workSiteId = adminworksiteId.ConvertAll(d => d.Id) }))
            using (var dbReader = db.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
            {
                var t = dbReader.ToEntities<DriverCompleteTask>();
                dbReader.NextResult();
                dbReader.JoinEntities(t, d => d.DriverDetail, d => d.WorkSiteId, "WorkSiteId");
                return t as List<DriverCompleteTask>;

            }
        }



        private class OrderResultData
        {
            public string Name { get; set; }
            public string PlateNumber { get; set; }
            public string SoilType { get; set; }
            public double Price { get; set; }
            public int TotalCount { get; set; }
        }
        public class DriverCompleteTask
        {
            [JsonIgnore]
            public int WorkSiteId { get; set; }
            public string WorkSite { get; set; }
            public int Sum { get { return TaskSum + TaskSum2; } }
            public int Price { get { return DriverPrice + DriverPrice2 - ExtraCost - ExtraCost2; } }
            public int WorkSitePrice { get { return WorkPrice2 + WorkPrice; } }
            public int TaskSum { get; set; }
            [JsonIgnore]
            public int DriverPrice { get; set; }
            [JsonIgnore]
            public int WorkPrice { get; set; }

            [JsonIgnore]
            public int ExtraCost { get; set; }
            [JsonIgnore]
            public int TaskSum2 { get; set; }
            [JsonIgnore]
            public int DriverPrice2 { get; set; }
            [JsonIgnore]
            public int WorkPrice2 { get; set; }
            [JsonIgnore]
            public int ExtraCost2 { get; set; }
            public List<DriverCompleteTaskDetail> DriverDetail { get; set; } = new List<DriverCompleteTaskDetail>();

        }
        public class DriverCompleteTaskDetail
        {
            public string Dump { get; set; }
            public string SoilType { get; set; }
            [JsonIgnore]
            public string Tas private class OrderResultData
        {
            public string Name { get; set; }
            public string PlateNumber { get; set; }
            public string SoilType { get; set; }
            public double Price { get; set; }
            public int TotalCount { get; set; }
        }kIdList { get; set; }
            public string[] TaskId
            {
                get
                {
                    return TaskIdList.Split(',');
                }
            }
            public int Price { get { return DriverPrice + DriverPrice2 - ExtraCost - ExtraCost2; } }
            public int TaskCount { get; set; }
            [JsonIgnore]
            public int DriverPrice { get; set; }
            [JsonIgnore]
            public int ExtraCost { get; set; }
            [JsonIgnore]
            public int TaskCount2 { get; set; }
            [JsonIgnore]
            public int DriverPrice2 { get; set; }
            [JsonIgnore]
            public int ExtraCost2 { get; set; }
            public int TaskSum { get { return TaskCount + TaskCount2; } }
        }
        public class ShortcutData
        {
            public int TaskStatus { get; set; }
            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string DriverName { get; set; }
            public string Dump { get; set; }
            public string Worksite { get; set; }
            public string PlateNumber { get; set; }
        }

        /// <summary>
        /// 获取对应名称的ID
        /// </summary>
        private ApiResult<dynamic> GetId(string name, int companyId, NameTypeEnum NameType)
        {
            string executeSql = null;
            switch (NameType)
            {
                case NameTypeEnum.DriverName:
                    executeSql = Sql.GetDriverNameSql; break;
                case NameTypeEnum.WorkSite:
                    executeSql = Sql.GetWorkSiteNameSql; break;
                case NameTypeEnum.KillSile:
                    executeSql = Sql.GetKillSiteNameSql; break;
                default: executeSql = null; break;
            }
            if (executeSql == null)
            {
                return ApiResult.Create(false, "请选择执行Sql");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {

                var id = c.Query<int>(executeSql, new { name, companyId });
                if (id.Count() == 0)
                {
                    return ApiResult.Create(false, "数据库不存在指定数据");
                }
                return new ApiResult<dynamic>() { Status = true, Result = id };
            }
        }

        public enum NameTypeEnum
        {
            /// <summary>
            /// 司机名称
            /// </summary>

            DriverName,
            /// <summary>
            /// 工地名称
            /// </summary>
            WorkSite,
            /// <summary>
            /// 消纳场名称
            /// </summary>
            KillSile
        }
        public enum DetailType
        {
            Driver,
            WorkSite,
            Dump,
            Shortcut
        }
        class WorkSiteResult
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Price { get; set; }
            public int BossPrice { get; set; }
            public int KillPrice { get; set; }
            public int TaskCount { get; set; }
        }
        class DumpResult
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
            public int Price { get; set; }
        }
        class DriverResult
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Count { get; set; }
            public int Price { get; set; }
        }
        class ParameterData
        {
            public int DriverId { get; set; }
            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }
        }
        class OrderStatistics
        {
            public int OrderId { get; set; }
            public string OrderName { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime DueTime { get; set; }
            public int Boss { get; set; }
            public int Work { get; set; }
            [JsonIgnore]
            public List<TaskData> TaskDetail { get; set; }
        }
        class TaskData
        {
            public int OrderId { get; set; }
            public int TaskId { get; set; }
            public int BossPrice { get; set; }
            public int DriverPrice { get; set; }
            public int WorkPrice { get; set; }
        }
        class WorkSite
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<Taskdata> Count { get; set; }
        }
        class Taskdata
        {
            public int TaskCount { get; set; }
            [JsonIgnore]
            public int Id { get; set; }
        }
        class DriverStatistics
        {
            public int DriverId { get; set; }
            public string DriverName { get; set; }
            public List<DriverTaskDetail> TaskDetail { get; set; }
        }
        class DriverTaskDetail
        {
            public int TaskId { get; set; }
            public DateTime CreateDate { get; set; }
            public string Pos { get; set; }
            public string TaskStep { get; set; }
            public string ImageUrl { get; set; }
        }
        [System.Diagnostics.DebuggerDisplay("{TaskId}")]
        public class StatisticsDetail
        {
            public string UnloadingRemark { get; set; }
            public string LoadingRemark { get; set; }
            public int IsExistEvidence { get; set; }
            public string SoilType { get; set; }
            public string LoadingAddress { get; set; }
            public string UnloadingAddress { get; set; }
            public List<string> LoadingUrl { get { return LoadingImages.ConvertAll(i => i.ImageUrl); } }
            public List<string> UnloadingUrl { get { return UnloadingImages.ConvertAll(i => i.ImageUrl); } }
            [JsonIgnore]
            public List<ImageData> LoadingImages { get; set; } = new List<ImageData>();
            [JsonIgnore]
            public List<ImageData> UnloadingImages { get; set; } = new List<ImageData>();
            [JsonIgnore]
            public int LoadingTaskId { get; set; }
            [JsonIgnore]
            public int UnloadingTaskId { get; set; }
            public int TaskId { get; set; }
            public string TaskNumber { get; set; }
            public string Name { get; set; }
            public DateTime LoadingTime { get; set; }
            public string Dump { get; set; }
            public DateTime UnloadTime { get; set; }
            public string PlateNumber { get; set; }
            public string Driver { get; set; }
            public int DriverPrice { get; set; }
            public int WorkSitePrice { get; set; }
            public int DumpPrice { get; set; }


            public int ExtraCost { get; set; }
            public bool IsExists { get; set; }
            public int UnitPrice { get; set; }
        }
    }
}


