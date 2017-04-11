using AppHelpers;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/Flow")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class FlowController : ApiController
    {
        public enum FlowType
        {
            审核, 报销
        }
        /// <summary>
        /// 获取现有审核流
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetFlow")]
        public object GetFlow(string token, FlowType type)
        {

            //TODO获取现有流程，并且获取消纳场审核的所有人员，以及其他流程的人员信息
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var r = c.Select<object>(Sql.GetFlowSql, new { type = type.ToString() });
                return ApiResult.Create(true, r);
            }
        }

        /// <summary>
        /// 增加审核步骤并分配员工权限
        /// </summary>
        /// <param name="flowName">新增流程名称</param>
        /// <returns></returns>

        [HttpPost, Route("AddFlow")]
        public object AddFlow(AddFlowData data)
        {

            return ApiResult.Create(false, "功能正在完善");

            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                //TODO增加员工分配
                return c.Update(Sql.AddFlowSql, new { data.FlowName, type = data.Type.ToString() }) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");

            }
        }

        /// <summary>
        /// 删除审核流
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("DetFlow")]
        public object DetFlow()
        {
            if (Request.Headers.Host.IndexOf("192.168.0.20") == -1)
            {
                return ApiResult.Create(false, "功能正在完善");
            }
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                //TODO 删除给流程分配的组
                return c.Update(Sql.DetFlowSql) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");

            }
        }

        public class AddFlowData
        {
            public FlowType Type { get; set; }
            public string FlowName { get; set; }
            public int CompanyId { get; set; }
            public int ForwardFlow { get; set; }
        }
    }
}
