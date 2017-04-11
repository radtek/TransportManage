using AppHelpers;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;
using TransportManage.Utilities;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    [RoutePrefix("api/Admin")]
    public class AdminController : ApiController
    {

        /// <summary>
        /// 获取工地权限部门分享
        /// </summary>
        /// <param name="workSiteId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetWorkSiteShare")]
        public dynamic GetWorkSiteShare(int workSiteId, string token)
        {

            using (var userInfoData = new UserInfoData(Request))

            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {

                    return c.SelectJson(Sql.GetWorkSiteShare, new { workSiteId }).CreateCustomResponseMessage();
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
            }
        }
        /// <summary>
        /// 删除工地权限部门分享
        /// </summary>
        /// <param name="worksiteId"></param>
        /// <param name="deptId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("DelWorkSiteShare")]
        public dynamic DelWorkSiteShare(int worksiteId, List<int> deptId, string token)
        {

            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    var delData = deptId.ConvertAll(d =>
                    {
                        return new
                        {
                            departmentId = d,
                            worksiteId = worksiteId
                        };
                    });
                    if (c.Execute(Sql.DelWorkSiteShare, delData) != 0)
                    {
                        return new { Status = true, Message = "操作成功" };
                    }
                    return new { Status = false, Message = "操作失败" };
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
            }

        }

        /// <summary>
        /// 新增工地权限部门分享
        /// </summary>
        /// <param name="worksiteId"></param>
        /// <param name="deptId"></param>
        /// <returns></returns>
        [HttpPost, Route("AddWorkSiteShare")]
        public dynamic AddWorkSiteShare(int worksiteId, List<int> deptId, string token)
        {

            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    var addData = deptId.ConvertAll(d =>
                    {
                        return new
                        {
                            ShareDepartmentId = d,
                            WorkSiteId = worksiteId
                        };
                    });
                    c.BulkCopy(addData, "ShareGroupDetail", SqlBulkCopyOptions.CheckConstraints);
                    return new { Status = true, Message = "操作成功" };

                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
            }
        }

        /// <summary>
        /// 获取工地管理员
        /// </summary>
        /// <param name="workSiteId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetWorkSiteAdmin")]
        public dynamic GetWorkSiteAdmin(int workSiteId, string token)
        {
            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    using (var d = c.SetupCommand(Sql.GetWorkSiteAdmin, new { workSiteId }))
                    using (var r = d.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                    {
                        var adminData = r.ToEntities<WorkSiteAdmin>();
                        adminData.ForEach(ed => ed.Detail = new List<EmployeeData>());
                        r.NextResult();
                        r.JoinEntities(adminData, ed => ed.Detail, ad => ad.Id, "DepartmentId");
                        return adminData;
                    }
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
            }
        }

        /// <summary>
        /// 删除工地管理员
        /// </summary>
        /// <param name="workSiteId"></param>
        /// <param name="EmployeeId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("DeleteWorkSiteAdmin")]
        public dynamic DeleteWorkSiteAdmin(int workSiteId, List<int> EmployeeId, string token)
        {
            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    var deleteData = EmployeeId.ConvertAll(d =>
                    {

                        return new
                        {
                            WorkSiteId = workSiteId,
                            EmployeeId = d
                        };
                    });
                    var tr = c.OpenTransaction();
                    if (c.Execute(Sql.DeleteWorkSiteAdmin, deleteData, tr) > 0)
                    {
                        var r = c.Execute(Sql.DeleteEmployeeLevel, deleteData.ConvertAll(
                            d =>
                            {
                                return new { EmployeeId = d.EmployeeId };
                            }), tr);
                        if (r > 0)
                        {
                            tr.Commit();
                            tr.Dispose();
                            return new
                            {
                                Status = true,
                                Message = "操作成功"
                            };
                        }
                    }
                    tr.Dispose();
                    return new
                    {
                        Status = false,
                        Message = "操作失败"
                    };
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }

            }
        }

        /// <summary>
        /// 新增工地管理员
        /// </summary>
        /// <param name="workSiteId"></param>
        /// <param name="EmployeeId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("AddWorkSiteAdmin")]
        public dynamic AddWorkSiteAdmin(int workSiteId, List<int> EmployeeId, string token)
        {
            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    var tr = c.OpenTransaction() as SqlTransaction;
                    try
                    {
                        var insertdata = EmployeeId.ConvertAll(d =>
                        {

                            return new
                            {
                                WorkSiteId = workSiteId,
                                EmployeeId = d
                            };
                        });
                        tr.BulkCopy(insertdata, "WorkSiteAdminGroup", SqlBulkCopyOptions.CheckConstraints);
                        //验证EmployeeId 是否已经又工管权限
                        tr.BulkCopy(insertdata.ConvertAll(d =>
                        {
                            return new
                            {
                                Name = "工管",
                                EmployeeId = d.EmployeeId
                            };
                        }), "EmployeeLevel", SqlBulkCopyOptions.CheckConstraints);

                        tr.Commit();
                        tr.Dispose();
                        return new
                        {
                            Status = true,
                            Message = "操作成功"
                        };
                    }
                    catch (Exception e)
                    {

                        tr.Dispose();
                        return new
                        {
                            Status = false,
                            Message = "操作失败"
                        };
                    }
                }
                else
                    return ApiResult.Create(false, "此接口只允许超管调用");

            }

        }

        /// <summary>
        /// 删除泥种
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("DelSoilType")]
        public dynamic DelSoilType(int id, string token)
        {
            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    if (c.Execute(Sql.DelSoilType, new { id }) > 0)
                    {
                        return new
                        {
                            Status = true,
                            Message = "操作成功"
                        };
                    }
                    return new
                    {

                        Status = false,
                        Message = "操作失败"
                    };
                }
                else
                    return ApiResult.Create(false, "此接口只允许超管调用");
            }
        }

        /// <summary>
        /// 修改泥种
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpPost, Route("ModifySoilType")]
        public dynamic ModifySoilType(int id, string name, string token)
        {
            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    if (c.Execute(Sql.ModifySoilType, new { }) > 0)
                    {
                        return new
                        {
                            Status = true,
                            Message = "操作成功"
                        };
                    }
                    return new
                    {
                        Status = false,
                        Message = "操作失败"
                    };
                }
                else
                    return ApiResult.Create(false, "此接口只允许超管调用");
            }
        }

        /// <summary>
        /// 新增泥种
        /// </summary>
        /// <param name="name"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("AddSoilType")]
        public dynamic AddSoilType(string name, string token)
        {
            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss || userInfoData.IsAdmin)
                {
                    var companyId = Request.Properties["CompanyId"];
                    if (c.Execute(Sql.AddSoilType, new { name, companyId }) > 0)
                    {
                        return new
                        {
                            Status = true,
                            Message = "操作成功"
                        };
                    }
                    return new
                    {
                        Status = false,
                        Message = "操作失败"
                    };
                }
                else
                    return ApiResult.Create(false, "此接口只允许超管或工管调用");
            }
        }

        /// <summary>
        /// 获取聊天
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        [HttpGet, Route("GetDialogue")]
        public dynamic GetDialogue(string token, int taskId)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                using (var dd = c.SetupCommand(Sql.GetDialogue, new { taskId }))
                using (var r = dd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                {
                    var dialogue = r.ToEntities<Dialogue>();
                    dialogue.ForEach(g => g.EData = new List<EvidenceData>());
                    r.NextResult();
                    r.JoinEntities(dialogue, d => d.EData, d => d.Id, "DialogueId");
                    return dialogue;
                }
            }
        }

        /// <summary>
        /// 审核退回
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("ReturnTask")]
        public dynamic ReturnTask(List<ReturnData> data, string token)
        {

            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss || userInfoData.IsAdmin)
                {
                    var taskId = data.ConvertAll(d => d.TaskId);
                    var tr = c.OpenTransaction();
                    var ud = c.Execute(Sql.ReturnTaskSql, new { taskId }, tr);
                    if (ud != data.Count)
                    {
                        tr.Dispose();
                        return new { Status = false, Message = "修改数据次数不一致" };
                    }
                    string type = null;
                    if (userInfoData.IsBoss)
                    {
                        type = "超管";
                    }
                    if (userInfoData.IsAdmin)
                    {
                        type = "工管";
                    }
                    var userId = Convert.ToInt32(Request.Properties["Id"]);
                    var dl = data.ConvertAll(d =>
                    {
                        return new
                        {
                            TaskId = d.TaskId,
                            Remark = d.Remark,
                            UserId = userId,
                            UserType = type == "超管" ? 1 : type == "工管" ? 2 : 0
                        };
                    });

                    var r = c.Execute(Sql.InsertDialogue, dl, tr);
                    if (r != data.Count)
                    {
                        return new { Status = false, Message = "插入数据次数不一致" };
                    }
                    tr.Commit();
                    tr.Dispose();
                    return new
                    {
                        Status = true,
                        Message = "操作成功"
                    };
                }
                else
                    return new { Status = false, Message = "此借口只允许超管登陆或工管" };
            }
        }

        /// <summary>
        /// 生成订单可选多个泥土类型，多个消纳场
        /// </summary>
        /// <param name="data">订单数据</param>
        /// <param name="token">token</param>
        /// <returns></returns>
        [HttpPost, Route("CreateOrder")]
        public dynamic CreateOrder(OrderData data, string token)
        {
            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsAdmin || userInfoData.IsBoss)
                {
                    if (data.SelectConfig.TrueForAll(d => data.WorkSitePrice <= (d.DumpPrice + d.DriverPrice) ? true : false))
                    {
                        return new ApiResult<string>() { Status = false, Result = "消纳场价格、司机价格总和大于工地价格" };
                    }
                    if (data.DueTime == null)
                    {
                        return new ApiResult<string>() { Status = false, Result = "订单过期时间不能为空" };
                    }
                    if (data.WorkSiteId == 0)
                    {
                        return new ApiResult<string>() { Status = false, Result = "工地ID不能为空" };
                    }
                    if (data.WorkSitePrice == 0)
                    {
                        return new ApiResult<string>() { Status = false, Result = "工地价格不能为空" };
                    }

                    if (data.SelectConfig.TrueForAll(d => d.DriverPrice <= 0 ? true : d.KillSoilId <= 0 ? true : d.SoilTypeId <= 0 ? true : d.DumpPrice <= 0 ? true : false))
                    {
                        return new ApiResult<string>() { Status = false, Result = "关键字为空或非法" };
                    };
                    if (data.CreateTime == null)
                    {
                        data.CreateTime = DateTime.Now;
                    }

                    c.Open();
                    var t = c.BeginTransaction();
                    var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                    var insertOrder = t.SetupCommand(Sql.CreateOrder, new { data.Name, DueTime = data.DueTime.Value.AddDays(1).AddSeconds(-1), data.CreateTime, CompanyId = companyId, Operater = Convert.ToInt32(Request.Properties["Id"]) }).SelectScalar<int>();
                    var listTemp = data.SelectConfig.ConvertAll(item => new
                    {
                        KillMudstoneSiteId = item.KillSoilId,
                        WorkSiteId = data.WorkSiteId,
                        SoilTypeId = item.SoilTypeId,
                        CompanyId = companyId,
                        OrderDetailStatus = 0,
                        BossPrice = data.WorkSitePrice - item.DriverPrice - item.DumpPrice,
                        WorkPrice = data.WorkSitePrice,
                        OrderId = insertOrder,
                        DriverPrice = item.DriverPrice,
                        DumpPrice = item.DumpPrice
                    });

                    //验证工地名称
                    var result = t.SetupCommand(Sql.CreateOrderDetail).Update(listTemp);
                    //var result = c.Execute(Sql.CreateOrderDetail, listTemp, t);

                    // var result = t.SetupCommand(Sql.CreateOrderDetail).Update(listTemp);
                    if (result == 0)
                    {
                        return new ApiResult<string>() { Status = false, Result = "新增订单失败" };
                    }
                    t.Commit();
                    t.Dispose();
                    c.Close();
                    return new ApiResult<string>() { Status = true, Result = "新增订单成功" };
                }
                else
                    return new { Status = false, Message = "此借口只允许超管登陆或工管" };
            }
        }

        /// <summary>
        /// 获取登陆老板公司的消纳场、工地、泥土类型信息
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetConfig")]
        public dynamic GetConfig(string token)
        {


            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var id = Convert.ToInt32(Request.Properties["CompanyId"]);

                string getDumpSql = null;
                string getWorksiteSql = null;
                string getSoilType = null;

                if (userInfoData.IsBoss)
                {
                    getWorksiteSql = Sql.GetBossWorkSite;

                }
                else if (userInfoData.IsAdmin & !userInfoData.IsBoss)
                {
                    getWorksiteSql = Sql.GetAdminWorksite;
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管或工管登陆");
                }
                //return ("{\"KillSite:\" + c.SelectJson(Sql.GetKillSite, new { id }) + ",\"workSite\":" + c.SelectJson(getWorksiteSql, new { id, employeeId = Convert.ToInt32(Request.Properties["Id"]) }) + ",'Soil':" + c.SelectJson(Sql.GetSoilType, new { id }) + "}").CustomResponMessage();


                return new ConfigData()
                {
                    KillSite = c.Select<DataList>(Sql.GetKillSite, new { id }),
                    WorkSite = c.Select<DataList>(getWorksiteSql, new { id, employeeId = Convert.ToInt32(Request.Properties["Id"]) }),
                    Soil = c.Select<DataList>(Sql.GetSoilType, new { id })
                };

            }
        }

        /// <summary>
        /// 获取公司订单信息
        /// </summary>
        /// <param name="token">token</param>
        /// <returns></returns>
        [HttpGet, Route("GetOrder")]
        public dynamic GetOrder(string token)
        {
            using (var userInfoData = new UserInfoData(Request))
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                string getOrderSql = Sql.GetAdminOrder;
                if (userInfoData.IsAdmin & !userInfoData.IsBoss)
                {
                    var employeeId = Request.Properties["Id"];
                    getOrderSql = getOrderSql.Replace("/*condition*/", " AND t.WorkSiteId IN @worksiteId");
                    var tr = c.Query<TransportManage.Controllers.AdminController.DataList>(Sql.GetAdminWorksite, new { employeeId }).AsList();
                    return ApiResult.Create(true, c.Query(getOrderSql, new { worksiteId = tr.ConvertAll(d => d.Id) }));
                }
                else if (userInfoData.IsBoss)
                {
                    return ApiResult.Create(true, c.Query(getOrderSql));
                }
                else
                {
                    return ApiResult.Create(false, "此借口只允许超管或工管调用");
                }
            }
        }


        /// <summary>
        /// 获取订单详细配置
        /// </summary>
        /// <param name="orderId">订单Id</param>
        /// <returns></returns>
        [HttpGet, Route("GetOrderDetail")]
        public dynamic GetOrderDetail(int orderId, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var result = c.Select<OrderDetail>(Sql.GetOrderDetail, new { orderId });
                return new
                {
                    OrderStatus = result[0].isExpire,
                    Result = new
                    {
                        WorkSiteName = result[0].WorkSiteName,
                        WorkSitePrice = result[0].WorkPrice,
                        CreateTime = result[0].CreateTime,
                        DueTime = result[0].DueTime,
                        Detail = result
                    }
                };
            }
        }

        /// <summary>
        /// 老板录入订单备注
        /// </summary>
        /// <param name="data">订单备注信息</param>
        /// <returns></returns>
        [HttpPost, Route("InsertTaskRemark")]
        public dynamic InsertTaskRemark(List<RemarkData> data, string token)
        {

            if (data.TrueForAll(d => d.Cost == 0 ? true : d.Date == null ? true : String.IsNullOrWhiteSpace(d.Remark) ? true : string.IsNullOrWhiteSpace(d.TaskNum) ? true : false))
            {
                return new ApiResult<string>() { Status = false, Result = "关键字不能为空" };
            }

            if (data == null)
            {
                return new ApiResult<string>() { Status = false, Result = "data is null" };
            }

            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                for (int i = 0; i < data.Count; i++)
                {
                    data[i].TaskNum = data[i].TaskNum.Trim();
                }
                var taskNumList = data.ConvertAll(d => d.TaskNum);
                var r = c.Query<int>(Sql.GetTaskNumStatus, new { taskNumList }).AsList();
                if (r == null)
                {
                    return ApiResult.Create(false, "请检查任务编号");
                }
                if (r.Contains(4) || r.Contains(3))
                {
                    return new ApiResult<string>() { Status = false, Result = "订单备注有任务已经被审核，请检查数据" };
                }
                var result = c.SetupCommand(Sql.CreateRemark).Update(data);

                if (result != data.Count)
                {
                    return new ApiResult<string>() { Status = false, Result = "录入存储备注数量不一致" };
                }
                return new ApiResult<string>() { Status = true, Result = "录入成功" };
            }
        }
        /// <summary>
        /// 获取报备信息
        /// </summary>
        /// <param name="taskNum">任务码,如若getType选择2那么，swagger中的taskNum可任务填写都会获取所有报备信息，如果URL调用则TaskNum值可不填,但字段还是要填例如"taskNum="</param>
        /// <param name="getType">1为某个，2为所有</param>
        /// <returns></returns>
        [HttpGet, Route("GetRemark")]
        public dynamic GetRemark(string token, int getType, string taskNum)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (getType == 1)
                {
                    return c.SetupCommand(Sql.GetRemark, new { taskNum }).Select();

                }
                else
                {
                    return c.SetupCommand(Sql.GetAllRemark).Select();
                }

            }
        }
        /// <summary>
        /// 审核任务
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("CheckTask")]
        public dynamic CheckTask(List<TaskIdData> data, string token)
        {
            if (data.Count > 1)
            {
                return new { Status = false, Message = "暂时只能审核一张单" };

            }
            var userInfoData = new UserInfoData(Request);
            if (userInfoData.IsBoss || userInfoData.IsAdmin)
            {
                if (data == null)
                {
                    return new ApiResult<string>() { Status = false, Result = "Data 为空" };
                }
                using (var c = AppDb.CreateConnection(AppDb.TransportManager))
                {
                    string result = null;
                    if (userInfoData.IsAdmin)
                    {

                        result = c.ExecuteScalar<string>(Sql.AdminCheckTask, new { EmployeeId = Convert.ToInt32(Request.Properties["Id"]), TaskId = data[0].TaskId });
                    }
                    else
                    {
                        result = c.ExecuteScalar<string>(Sql.BossCheckTask, new { TaskId = data[0].TaskId });
                    }
                    if (result == "defeated")
                    {
                        return new { Status = false, Message = "操作失败" };
                    }
                    return new { Status = true, Message = "操作成功" };
                }
            }
            return new { Status = false, Message = "此借口只允许超管登陆或工管" };


        }

        /// <summary>
        /// 获取时间段已完成的任务(时间为Null，返回所有完成的任务)
        /// </summary>
        /// <param name="token"></param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns></returns>
        [HttpGet, Route("GetCompleteTask")]
        public dynamic GetCompleteTask(string token, string startTime, string endTime)
        {
            if (startTime == null & endTime == null)
            {
                return GetTimeIntervalCompleteTask(token);
            }
            else if (startTime == null & endTime != null)
            {
                return ApiResult.Create(false, "参数异常");
            }
            else if (startTime != null & endTime == null)
            {
                return ApiResult.Create(false, "参数异常");
            }
            else
            {
                return GetTimeIntervalCompleteTask(token, startTime, endTime);
            }

        }

        /// <summary>
        /// 获取系统已完成的订单，时间缺省参数
        /// </summary>
        /// <param name="token">token</param>
        /// <returns>时间缺省的状态下返回2000/1/1至3000/1/1内的任务</returns>
        [HttpGet, Route("GetTimeIntervalCompleteTask")]
        public dynamic GetTimeIntervalCompleteTask(string token, string startTime = "2000/1/1", string endTime = "3000/1/1")
        {
            var usertype = Request.Properties["UserType"].ToString();
            if (usertype == "Driver")
            {
                return new ApiResult<string>() { Status = false, Result = "司机没有权限调用此接口" };
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var id = Convert.ToInt32(Request.Properties["Id"]);
                //var result = c.Query<StatisticsDetail>(Sql.GetAllComplete, new { id });
                using (var db = c.SetupCommand(Sql.GetAllComplete, new { id }))
                using (var taskReader = db.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                {
                    var r = taskReader.ToEntities<StatisticsDetail>();
                    taskReader.NextResult();
                    taskReader.JoinEntities(r, d => d.LoadingUrl, d => d.LoadingTaskDetailId, "TaskDetailId");
                    taskReader.NextResult();
                    taskReader.JoinEntities(r, d => d.UnloadingUrl, d => d.UnloadingTaskDetailId, "TaskDetailId");
                    r.ForEach(d =>
                    {
                        if (d.ExtraCost != 0)
                        {
                            d.IsExists = true;
                        }
                    });
                    var result = r.Where(d =>
                       {
                           if (d.UnloadingTime < Convert.ToDateTime(endTime).AddDays(1).AddMinutes(-1) && d.UnloadingTime > Convert.ToDateTime(startTime))
                           {
                               return true;
                           }
                           else
                           {
                               return false;
                           }
                       }
                         );
                    return new { Status = true, Message = "获取系统所有任务为2的订单", Result = result };
                }
            }
        }

        /// <summary>
        /// 获取司机上传图片信息
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        [HttpGet, Route("GetDeliveryTaskPhote")]
        public dynamic GetDeliveryTaskPhote(int taskId, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                return c.Query(Sql.GetDriverPhoto, new { taskId });
            }
        }



        class WorkSiteAdmin
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<EmployeeData> Detail { get; set; }
        }
        class EmployeeData
        {
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; }
            public int IsAdmin { get; set; }
        }
        public class ReturnData
        {
            public int TaskId { get; set; }
            public string Remark { get; set; }
            [JsonIgnore]
            public int UserId { get; set; }
            [JsonIgnore]
            public int UserType { get; set; }
        }
        class Dialogue
        {
            [JsonIgnore]
            public int Id { get; set; }
            public string Remark { get; set; }
            [JsonIgnore]
            public List<EvidenceData> EData { get; set; }
            public DateTime? CreateTime { get; set; }
            public string Name { get; set; }
            public int DialogueType { get; set; }
            public List<string> Url { get { return EData.ConvertAll(i => i.Url); } }
        }
        class EvidenceData
        {
            //public int DialogueId { get; set; }
            public string Url { get; set; }

        }
        public class StatisticsDetail
        {
            [JsonIgnore]
            public int LoadingTaskDetailId { get; set; }
            [JsonIgnore]
            public int UnloadingTaskDetailId { get; set; }
            public int TaskId { get; set; }
            public string TaskNumber { get; set; }
            public string Name { get; set; }
            public DateTime LoadingTime { get; set; }
            public string Dump { get; set; }
            public DateTime UnloadingTime { get; set; }
            public string PlateNumber { get; set; }
            public string DriverName { get; set; }
            public int DriverPrice { get; set; }
            public int WorkSitePrice { get; set; }
            public int DumpPrice { get; set; }
            //public string LoadingUrl { get; set; }
            // public string UnloadingUrl { get; set; }
            public int ExtraCost { get; set; }
            public bool IsExists { get; set; }
            public string LoadingAddress { get; set; }
            public string UnloadingAddress { get; set; }
            public bool IsExistEvidence { get; set; }
            public List<string> LoadingUrl { get; set; } = new List<string>();
            public List<string> UnloadingUrl { get; set; } = new List<string>();
        }
        public class TaskIdData
        {
            public int TaskId { get; set; }

        }
        public class RemarkData
        {
            public string TaskNum { get; set; }
            public DateTime? Date { get; set; }
            public string Remark { get; set; }
            public int Cost { get; set; }
        }
        class OrderDetail
        {
            public int BossPrice { get; set; }
            public string WorkSiteName { get; set; }
            public int Id { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime DueTime { get; set; }
            public string Name { get; set; }
            public int WorkPrice { get; set; }
            public int isExpire { get; set; }
            public int orderDetailID { get; set; }
            public int OrderDetailStatus { get; set; }
            public string Dump { get; set; }
            public int DumpPrice { get; set; }
            public string SoilType { get; set; }
            public int DriverPrice { get; set; }
        }
        class AdminOrder
        {
            public int Id { get; set; }
            public string OrderName { get; set; }
            public string WorkSite { get; set; }
            public int Price { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime DueTime { get; set; }
        }
        public class OrderData
        {

            public int CompanyId { get; set; }
            public int WorkSiteId { get; set; }
            public double WorkSitePrice { get; set; }
            public List<SelectData> SelectConfig { get; set; }
            public string Name { get; set; }
            public DateTime? DueTime { get; set; }
            public DateTime? CreateTime { get; set; }
        }

        public class SelectData
        {
            public int SoilTypeId { get; set; }
            public double DumpPrice { get; set; }
            public int KillSoilId { get; set; }
            public double DriverPrice { get; set; }
        }
        class ConfigData
        {
            public IList<DataList> KillSite { get; set; }
            public IList<DataList> WorkSite { get; set; }
            public IList<DataList> Soil { get; set; }
        }

        public class DataList
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        class ModelData
        {
            public string PlateNumber { get; set; }
            public string Driver { get; set; }
            public string OpenId { get; set; }
        }
    }
}
