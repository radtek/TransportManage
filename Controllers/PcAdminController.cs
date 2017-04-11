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
        /// 增加权限
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("AddRole")]
        public object AddRole(AddRoleData data)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
                //if (data.RoleName == "司机")
                //{
                //    if (c.SelectScalar<int>(Sql.GetDriverCar, new { driverId = data.EmployeeId }) != data.EmployeeId.Count)
                //    {
                //        return ApiResult.Create(false, "员工不存在车辆，请确认后重新操作");
                //    }
                //}
                if (data.RoleName == "超管")
                {

                    var r = c.SetupCommand(Sql.AddAuthority).Update(data.EmployeeId.ConvertAll(d => new { EmployeeId = d, RoleName = data.RoleName }));
                    if (r > 0)
                    {
                        return ApiResult.Create(true, "操作成功");

                    }
                    return ApiResult.Create(false, "操作失败");
                }
                else
                {
                    return ApiResult.Create(false, "操作失败");
                }
            }
        }


        /// <summary>
        /// 获取权限
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetRoleList")]
        public object GetRoleList(string token)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
                return c.SelectJson<object>(Sql.GetRoleList, new { CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]) }).CreateJsonMessage();
            }
        }

        /// <summary>
        /// 获取权限人员
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetRoleMember")]
        public object GetRoleMember(string roleName, string token)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }

                return c.SelectJson<object>(Sql.GetRoleMember, new { RoleName = roleName, CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]) }).CreateJsonMessage();


            }
        }

        /// <summary>
        /// 删除权限
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("DeleteRole")]
        public object DeleteRole(DeleteRoleData data)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
                var r = c.Update(Sql.DeleteRole, new { employeeId = data.EmpolyeeId, roleName = data.RoleName });
                if (r != 0)
                {
                    return ApiResult.Create(true, "操作成功");
                }
                return ApiResult.Create(false, "操作失败");

            }
        }
        /// <summary>
        /// 获取系统上所有的司机以及车牌号
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetAllDriversAndVehicleNumber")]
        public object GetAllDriversAndVehicleNumber(string token)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Request.Properties["CompanyId"];
                if (userInfo.IsAdmin || userInfo.IsBoss)
                {
                    return new
                    {
                        Dirver = c.Query(Sql.GetAllDriver, new { companyId }),
                        VehicleNumber = c.Query(Sql.GetAllVehicleNumber, new { companyId })
                    };
                }
                else
                {
                    return ApiResult.Create(false, "此借口只允许超管或工管调用");
                }
            }
        }

        /// <summary>
        /// 获取工地权限部门分享
        /// </summary>
        /// <param name="workSiteId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetWorkSiteShare")]
        public object GetWorkSiteShare(int workSiteId, string token)
        {

            var userInfoData = new UserInfoData(Request);

            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {

                    return c.Select<object>(Sql.GetWorkSiteShare, new { workSiteId, companyId = Convert.ToInt32(Request.Properties["CompanyId"]) });
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
        public object DelWorkSiteShare(int worksiteId, List<int> deptId, string token)
        {

            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                if (userInfoData.IsBoss)
                {
                    var delData = deptId.ConvertAll(d =>
                    {
                        return new
                        {
                            departmentId = d,
                            worksiteId = worksiteId,
                            CompanyId = companyId
                        };
                    });
                    if (c.Execute(Sql.DelWorkSiteShare, delData) != 0)
                    {
                        return ApiResult.Create(true, "操作成功");
                    }
                    return ApiResult.Create(false, "操作失败");
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
        public object AddWorkSiteShare(int worksiteId, List<int> deptId, string token)
        {

            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);

                if (userInfoData.IsBoss)
                {
                    var addData = deptId.ConvertAll(d =>
                    {
                        return new
                        {
                            ShareDepartmentId = d,
                            WorkSiteId = worksiteId,
                            CompanyId = companyId
                        };
                    });
                    c.BulkCopy(addData, "ShareGroupDetail", SqlBulkCopyOptions.CheckConstraints);
                    return ApiResult.Create(true, "操作成功");

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
        public object GetWorkSiteAdmin(int workSiteId, string token)
        {
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    using (var d = c.SetupCommand(Sql.GetWorkSiteAdmin, new { workSiteId, companyId = Convert.ToInt32(Request.Properties["CompanyId"]) }))
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
        /// <param name="worksiteId"></param>
        /// <param name="employeeId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("DeleteWorkSiteAdmin")]
        public object DeleteWorkSiteAdmin(int worksiteId, List<int> employeeId, string token)
        {

            if (employeeId.Count > 1)
            {
                return ApiResult.Create(false, "暂时只允许操作一个员工");
            }
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Request.Properties["CompanyId"];

                if (userInfoData.IsBoss)
                {
                    var employees = employeeId.ConvertAll(d => new { EmployeeId = d });
                    using (var tr = c.OpenTransaction())
                    {
                        var dw = c.Execute(Sql.DeleteWorkSiteAdmin, new { WorksiteId = worksiteId, CompanyId = companyId, employeeId = employeeId[0] }, tr);
                        var de = c.Execute(Sql.DeleteEmployeeLevel, new { EmployeeId = employeeId[0] }, tr);
                        if (dw > 0 & de > 0)
                        {
                            tr.Commit();
                        }
                    }
                    return ApiResult.Create(true, "操作成功");
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
        /// <param name="employeeId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("AddWorkSiteAdmin")]
        public object AddWorkSiteAdmin(int workSiteId, List<int> employeeId, string token)
        {
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                if (userInfoData.IsBoss)
                {
                    var employees = employeeId.ConvertAll(d => new { EmployeeId = d });
                    c.Update("INSERT INTO WorkSiteAdminGroup (CompanyId, EmployeeId, WorkSiteId) values (@CompanyId, @EmployeeId, @WorkSiteId);INSERT INTO EmployeeLevel (Name, EmployeeId) values (@Name, @EmployeeId)", new { companyId, workSiteId, Name = "工管" }, employees);
                    return ApiResult.Create(true, "操作成功");
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
        public object DelSoilType(int id, string token)
        {
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Request.Properties["CompanyId"];

                if (userInfoData.IsBoss)
                {
                    if (c.Execute(Sql.DelSoilType, new { id, companyId }) > 0)
                    {
                        return ApiResult.Create(true, "操作成功");
                    }
                    return ApiResult.Create(false, "操作失败");
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
            }
        }

        /// <summary>
        /// 修改泥种
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpPost, Route("ModifySoilType")]
        public object ModifySoilType(int id, string name, string token)
        {
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss)
                {
                    if (c.Execute(Sql.ModifySoilType, new { id, companyId = Request.Properties["CompanyId"] }) > 0)
                    {
                        return ApiResult.Create(true, "操作成功");
                    }
                    return ApiResult.Create(false, "操作失败");
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管调用");
                }
            }
        }

        /// <summary>
        /// 新增泥种
        /// </summary>
        /// <param name="name"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("AddSoilType")]
        public object AddSoilType(string name, string token)
        {
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsBoss || userInfoData.IsAdmin)
                {
                    var companyId = Request.Properties["CompanyId"];
                    if (c.Execute(Sql.AddSoilType, new { name, companyId, Operater = Request.Properties["Id"] }) > 0)
                    {
                        return ApiResult.Create(true, "操作成功");
                    }
                    return ApiResult.Create(false, "操作失败");
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
        public object GetDialogue(string token, int taskId)
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
        public object ReturnTask(List<ReturnData> data, string token)
        {

            var userInfoData = new UserInfoData(Request);
            var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            using (var tr = c.OpenTransaction())
            {
                if (userInfoData.IsBoss || userInfoData.IsAdmin)
                {
                    var rd = data.ConvertAll(d => new { TaskId = d.TaskId, CompanyId = Request.Properties["CompanyId"] });
                    var ud = c.Execute(Sql.ReturnTaskSql, rd, tr);
                    if (ud != data.Count)
                    {
                        return ApiResult.Create(false, "修改数据次数不一致");
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
                            UserType = type == "超管" ? 1 : type == "工管" ? 2 : 0,
                            CompanyId = companyId
                        };
                    });

                    var r = c.Execute(Sql.InsertDialogue, dl, tr);
                    if (r != data.Count)
                    {
                        return ApiResult.Create(false, "插入数据次数不一致");
                    }
                    tr.Commit();
                    return ApiResult.Create(true, "操作成功");
                }
                else
                {
                    return ApiResult.Create(false, "此借口只允许超管登陆或工管");
                }
            }
        }

        /// <summary>
        /// 生成订单可选多个泥土类型，多个消纳场
        /// </summary>
        /// <param name="data">订单数据</param>
        /// <param name="token">token</param>
        /// <returns></returns>
        [HttpPost, Route("CreateOrder")]
        public object CreateOrder(OrderData data, string token)
        {

            if (data.DueTime == null)
            {
                return ApiResult.Create(false, "订单过期时间不能为空");
            }
            if (data.WorkSiteId == 0)
            {
                return ApiResult.Create(false, "工地ID不能为空");
            }
            if (data.CreateTime == null)
            {
                data.CreateTime = DateTime.Now;
            }
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            using (var t = c.OpenTransaction())
            {
                if (userInfoData.IsAdmin || userInfoData.IsBoss)
                {
                    var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                    var insertOrder = t.SetupCommand(Sql.CreateOrder, new { data.Name, DueTime = data.DueTime.Value.AddDays(1).AddSeconds(-1), data.CreateTime, CompanyId = companyId, Operater = Convert.ToInt32(Request.Properties["Id"]) }).SelectScalar<int>();
                    var listTemp = data.SelectConfig.ConvertAll(item => new
                    {
                        KillMudstoneSiteId = item.KillSoilId,
                        WorkSiteId = data.WorkSiteId,
                        SoilTypeId = item.SoilTypeId,
                        CompanyId = companyId,
                        OrderDetailStatus = 0,
                        BossPrice = data.WorkSitePrice + item.DriverPrice + item.DumpPrice,
                        WorkPrice = data.WorkSitePrice,
                        OrderId = insertOrder,
                        DriverPrice = item.DriverPrice,
                        DumpPrice = item.DumpPrice,
                        Operater = Convert.ToInt32(Request.Properties["Id"]),
                        ExtraPrice = item.ExtraPrice
                    });

                    //验证工地名称
                    var result = t.SetupCommand(Sql.CreateOrderDetail).Update(listTemp);
                    if (result == 0)
                    {
                        return ApiResult.Create(false, "新增订单失败");
                    }
                    t.Commit();
                    return ApiResult.Create(true, "新增订单成功");
                }
                else
                {
                    return ApiResult.Create(false, "此借口只允许超管登陆或工管");
                }
            }
        }

        /// <summary>
        /// 获取登陆老板公司的消纳场、工地、泥土类型信息
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetConfig")]
        public object GetConfig(string token)
        {


            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var id = Convert.ToInt32(Request.Properties["CompanyId"]);
                string getWorksiteSql = null;
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



                return new ConfigData()
                {
                    KillSite = c.Select<DataList>(Sql.GetKillSite, new { id }),
                    WorkSite = c.Select<DataList>(getWorksiteSql, new { id, employeeId = Convert.ToInt32(Request.Properties["Id"]) }),
                    Soil = c.Select<DataList>(Sql.GetSoilType, new { id })
                };

            }
        }

        /// <summary>
        /// 修改工地订单价格时间
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("ModifyOrder")]
        public object ModifyOrder(ModifyData data)
        {
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfoData.IsAdmin || userInfoData.IsBoss)
                {
                    return c.Update(Sql.ModifyOrderSql, data) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");
                }
                else
                {
                    return ApiResult.Create(false, "跨越权限调用");
                }
            }
        }

        /// <summary>
        /// 获取公司订单信息
        /// </summary>
        /// <param name="token">token</param>
        /// <returns></returns>
        [HttpGet, Route("GetOrder")]
        public object GetOrder(string token)
        {
            var userInfoData = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Request.Properties["CompanyId"];
                string getOrderSql = Sql.GetAdminOrder;
                if (userInfoData.IsAdmin & !userInfoData.IsBoss)
                {

                    var employeeId = Request.Properties["Id"];
                    getOrderSql = getOrderSql.Replace("/*condition*/", " AND t.WorkSiteId IN @worksiteId");
                    var tr = c.Query<DataList>(Sql.GetAdminWorksite, new { employeeId, id = companyId }).AsList();
                    return ApiResult.Create(true, c.Query(getOrderSql, new { worksiteId = tr.ConvertAll(d => d.Id), companyId }));
                }
                else if (userInfoData.IsBoss)
                {
                    return ApiResult.Create(true, c.Query(getOrderSql, new { companyId }));
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
        public object GetOrderDetail(int orderId, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var result = c.Select<OrderDetail>(Sql.GetOrderDetail, new { orderId, companyId = Convert.ToInt32(Request.Properties["CompanyId"]) });
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
        public object InsertTaskRemark(List<RemarkData> data, string token)
        {
            if (data == null)
            {
                return ApiResult.Create(false, "data is null");
            }

            if (data.TrueForAll(d => d.Cost == 0 ? true : d.Date == null ? true : String.IsNullOrWhiteSpace(d.Remark) ? true : string.IsNullOrWhiteSpace(d.TaskNum) ? true : false))
            {
                return ApiResult.Create(false, "关键字不能为空");
            }


            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);

                for (int i = 0; i < data.Count; i++)
                {
                    data[i].TaskNum = data[i].TaskNum.Trim();
                    data[i].CompanyId = companyId;
                }
                var r = c.Query<int>(Sql.GetTaskNumStatus, new { taskNumList = data.ConvertAll(d => d.TaskNum) }).AsList();
                if (r == null)
                {
                    return ApiResult.Create(false, "请检查任务编号");
                }
                if (r.Contains(4) || r.Contains(3))
                {
                    return ApiResult.Create(false, "订单备注有任务已经被审核，请检查数据");
                }
                var result = c.Update(Sql.CreateRemark, data);

                if (result != data.Count)
                {
                    return ApiResult.Create(false, "录入存储备注数量不一致");
                }
                return ApiResult.Create(true, "录入成功");
            }
        }
        /// <summary>
        /// 获取报备信息
        /// </summary>
        /// <param name="taskNum"></param>
        /// <param name="getType">1为某个，2为所有</param>
        /// <returns></returns>
        [HttpGet, Route("GetRemark")]
        public object GetRemark(string token, int getType, string taskNum)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                if (getType == 1)
                {
                    return c.Select<object>(Sql.GetRemark, new { taskNum, companyId });

                }
                else
                {
                    return c.Select<object>(Sql.GetAllRemark, new { companyId });
                }

            }
        }
        /// <summary>
        /// 审核任务
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("CheckTask")]
        public object CheckTask(List<TaskIdData> data, string token)
        {


            if (data.Count > 1)
            {
                return ApiResult.Create(false, "暂时只能审核一张单");
            }
            if (data == null)
            {
                return ApiResult.Create(false, "Data 为空");
            }

            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (!userInfo.IsDriver)
                {
                    var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                    var employeeId = Convert.ToInt32(Request.Properties["Id"]);
                    string result = null;
                    if (userInfo.IsAdmin)
                    {
                        //审核该用户是否有权限修改该 taskId 的状态，并记录操作时间以及操作人员
                        result = c.ExecuteScalar<string>(Sql.AdminCheckTask, new { EmployeeId = employeeId, TaskId = data[0].TaskId, CompanyId = companyId });
                    }
                    else
                    {
                        result = c.ExecuteScalar<string>(Sql.BossCheckTask, new { EmployeeId = employeeId, TaskId = data[0].TaskId, CompanyId = companyId });
                    }
                    if (result == "defeated")
                    {
                        return ApiResult.Create(false, "操作失败");
                    }
                    return ApiResult.Create(true, "操作成功");
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管或工管调用");
                }
            }
        }

        /// <summary>
        /// 获取时间段已完成的任务(时间为Null，返回所有完成的任务)
        /// </summary>
        /// <param name="token"></param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns></returns>
        [HttpGet, Route("GetCompleteTask")]
        public object GetCompleteTask(string token, string startTime, string endTime)
        {
            if ((startTime ?? endTime) == null)
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
        public object GetTimeIntervalCompleteTask(string token, string startTime = "2000/1/1", string endTime = "3000/1/1")
        {
            var userInfo = new UserInfoData(Request);
            if (userInfo.IsDriver)
            {
                return ApiResult.Create(false, "司机没有权限调用此接口");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                using (var db = c.SetupCommand(Sql.GetAllComplete, new { id = Convert.ToInt32(Request.Properties["Id"]), companyId = Convert.ToInt32(Request.Properties["CompanyId"]) }))
                using (var taskReader = db.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                {
                    var r = taskReader.ToEntities<StatisticsDetailData>();
                    var temp = r.Where(d =>
                    {//遍历前去除补录订单，否则会报相同键异常
                        return d.LoadingTaskDetailId != 0;
                    });

                    taskReader.NextResult();
                    taskReader.JoinEntities(temp, d => d.LoadingUrl, d => d.LoadingTaskDetailId, "TaskDetailId");
                    taskReader.NextResult();
                    taskReader.JoinEntities(temp, d => d.UnloadingUrl, d => d.UnloadingTaskDetailId, "TaskDetailId");
                    temp.ForEach(d =>
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
                           else if (d.LoadingTaskDetailId == 0)
                           {
                               return true;
                           }
                           else
                           {
                               return false;
                           }
                       }
                         );
                    return ApiResult.Create(true, result, "获取系统所有任务为2的订单");
                }
            }
        }

        /// <summary>
        /// 获取司机上传图片信息
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        [HttpGet, Route("GetDeliveryTaskPhote")]
        public object GetDeliveryTaskPhote(int taskId, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                return c.Query(Sql.GetDriverPhoto, new { taskId });
            }
        }


        /// <summary>
        /// 获取已完成的详细快捷入口
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("GetCompleteTaskShortCut")]
        public List<StatisticsDetailData> GetCompleteTaskShortCut(StatisticsController.ShortcutData data, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                data.TaskStatus = data.TaskStatus == 2 ? c.SelectScalar<int>(Sql.GetDumpCheckNumber, new { }) : c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                var r = MethodHelp.FuzzySearching(c, data, companyId);
                return r.ConvertAll(d => new StatisticsDetailData()
                {
                    DriverName = d.Driver,
                    Dump = d.Dump,
                    DriverPrice = d.DriverPrice,
                    DumpPrice = d.DumpPrice,
                    ExtraCost = d.ExtraCost,
                    IsExistEvidence = d.IsExistEvidence,
                    IsExists = d.IsExists,
                    LoadingAddress = d.LoadingAddress,
                    LoadingRemark = d.LoadingRemark,
                    LoadingTaskDetailId = d.LoadingTaskId,
                    LoadingTime = d.LoadingTime,
                    LoadingUrl = d.LoadingUrl,
                    Name = d.Name,
                    PlateNumber = d.PlateNumber,
                    TaskId = d.TaskId,
                    TaskNumber = d.TaskNumber,
                    UnloadingAddress = d.UnloadingAddress,
                    UnloadingRemark = d.UnloadingRemark,
                    UnloadingTaskDetailId = d.UnloadingTaskId,
                    UnloadingTime = d.UnloadTime,
                    UnloadingUrl = d.UnloadingUrl,
                    WorkSitePrice = d.WorkSitePrice

                });
            }
        }


        public class ModifyData
        {
            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public int WorkSitePrice { get; set; }
        }
        public class DeleteRoleData
        {
            public string RoleName { get; set; }
            public List<int> EmpolyeeId { get; set; }
        }
        private class WorkSiteAdmin
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<EmployeeData> Detail { get; set; }
        }
        private class EmployeeData
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
        [System.Diagnostics.DebuggerDisplay("{TaskId}")]
        public class StatisticsDetailData
        {
            public int IsExistEvidence { get; set; }
            public string LoadingRemark { get; set; }
            public string UnloadingRemark { get; set; }
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
            [JsonIgnore]
            public int CompanyId { get; set; }
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


            public int WorkSiteId { get; set; }
            public double WorkSitePrice { get; set; }
            public List<SelectData> SelectConfig { get; set; }
            public string Name { get; set; }
            public DateTime? DueTime { get; set; }
            public DateTime? CreateTime { get; set; }
        }

        public class SelectData
        {
            public int ExtraPrice { get; set; }
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

        public class AddRoleData
        {
            public string RoleName { get; set; }
            public List<int> EmployeeId { get; set; }
        }
        public class DataList
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        private class ModelData
        {
            public string PlateNumber { get; set; }
            public string Driver { get; set; }
            public string OpenId { get; set; }
        }
    }
}
