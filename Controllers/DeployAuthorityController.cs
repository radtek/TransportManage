using AppHelpers;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/DeployAuthority")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]

    public class DeployAuthorityController : ApiController
    {

        /// <summary>
        /// 分配角色权限
        /// </summary>
        /// <returns></returns>

        [HttpPost, Route("DRoleAuth")]
        public object DRoleAuth(DRoleData data, AuthType type, string token)
        {
            //TODO只允许超管才允许调用此接口

            //@超管 
            //@司机

            //以上是不对其分配资源,即枚举类型为奇数

            //@工管
            //@消管
            //判断是否已经存在权限组
            //以上需要对其分配工地、消纳场资源,即枚举类型为偶数


            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                //以下是分配组权限
                var auth = type.ToString();
                var t = c.Update(Sql.AuthSql, new { CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]), Name = Convert.ToBoolean((int)type & 1) ? auth + "组" : data.Data.Name + "组", type = auth }, data.EmployeeId.ConvertAll(d => new { EmployeeId = d }));
                if (t == 0)
                {
                    return ApiResult.Create(false, "新增权限失败");
                }
                //以下是分配工地资源、TODO分配消纳场资源



                return data != null & Convert.ToBoolean((int)type & 1) ? ApiResult.Create(true, "新增权限成功")
                      : c.Update(Sql.DRoleAuthSql, new { DepartmentId = t, SiteId = data.Data.SiteId, type = type.ToString() }) == 0 ? ApiResult.Create(false, "操作失败")
                      : ApiResult.Create(true, "操作成功");

                //todo 指派人员报销流程，指派人员审核流程
            }
        } //增


        /// <summary>
        /// 指派报销流程审核人员，指派审核任务订单人员
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("DesignateAuth")]
        public object DesignateAuth(DesignateData data, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                return c.Update(Sql.AddDesignateAuth, new { data.FlowId, CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]), data.EmployeeId }) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");
            }
        }//增



        /// <summary>
        /// 获取工地允许运输的员工、工地管理员
        /// </summary>
        /// <param name="type">输出结果类型</param>
        /// <param name="workSiteId">可选参数，缺省时返回类型所有数据，否则为单个ID类型数据</param>
        /// <returns></returns>
        [HttpGet, Route("GetWorkStieEmployee")]
        public object GetWorkStieEmployee(WorkSiteEmployeeType type, int workSiteId = 0)
        {
            return ApiResult.Create(false, "功能正在玩完善");

            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                string sql = null;
                if (workSiteId == 0)
                {
                    sql = Sql.GetAllWorkSiteEmployee;
                }
                else
                {
                    sql = Sql.GetOneWorkSiteEmployee;
                }
                var result = c.Select<object>(sql, new { CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]), type, workSiteId });
                return ApiResult.Create(true, result);
            }

        }//查

        /// <summary>
        /// 获取审核流程、报销流程中拥有权限的员工
        /// </summary>
        /// <param name="token"></param>
        /// <param name="type">输出结果类型</param>
        /// <param name="flowId">可选参数，缺省时返回类型所有数据，否则为单个ID类型数据</param>
        /// <returns></returns>
        [HttpGet, Route("GetFlowEmployee")]
        public object GetFlowEmployee(string token, FlowEmployeeType type, int flowId = 0)
        {
            return ApiResult.Create(false, "功能正在完善");
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                string sql = null;
                if (flowId == 0)
                {
                    sql = Sql.GetAllFlowEmployeeData;
                }
                else
                {
                    sql = Sql.GetOneFlowEmployeeData;
                }
                var result = c.Select<object>(sql, new { CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]), type, flowId });
                return ApiResult.Create(true, result);
            }


        }//查


        /// <summary>
        /// 修改权限
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("ModifiyRoleAuth")]
        public object ModifiyRoleAuth(ModifiyData data, string token)
        {
            return ApiResult.Create(false, "功能正在完善");
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                return c.Update(Sql.UpdateRoleAuth, data) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");
            }
        }//改


        public enum ModifiyDataType
        {
            工管, 指派, 超管, 司机, 审核, 报销
        }
        public class ModifiyData
        {
            public int EmployeeId { get; set; }
            public int GroupId { get; set; }
            public ModifiyDataType ModifiyType { get; set; }

        }

        public enum FlowEmployeeType
        {
            审核, 报销
        }

        public enum WorkSiteEmployeeType
        {
            指派,
            工管
        }
        public class DesignateData
        {
            public int FlowId { get; set; }
            public int EmployeeId { get; set; }
        }

        public class SiteData
        {
            public int SiteId { get; set; }
            public string Name { get; set; }
        }
        public class DRoleData
        {
            public SiteData Data { get; set; }
            public List<int> EmployeeId { get; set; }
        }
    }



    public enum AuthType
    {
        工管 = 2,
        指派 = 4,

        超管 = 1,
        司机 = 3
    }



}
