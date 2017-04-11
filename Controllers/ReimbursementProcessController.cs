using AppHelpers;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/ReimbursementProcess")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class ReimbursementProcessController : ApiController
    {
        /// <summary>
        /// 审核报销流程
        /// </summary>
        /// <param name="id">被审核ID</param>
        /// <returns></returns>
        [HttpGet, Route("CheckApplication")]
        public object CheckApplication(int id, string token)//增、改
        {
            // return ApiResult.Create(false, "功能正在完善");
            if (Request.Headers.Host.IndexOf("192.168.0.20") == -1)
            {
                return ApiResult.Create(false, "功能正在完善");
            }

            if (id == 0)
            {
                return ApiResult.Create(false, "参数不能为空");
            }

            //审核任务，若任务已经进入审核流那么审核流加一，若任务还没进入审核流那么添加此任务进审核流
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                return c.Update(Sql.CheckApplicationSql, new { id }) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");
            }
        }

        /// <summary>
        /// 获取正在报销流的订单
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public object GetApplicationDetail(string token)
        {//如何确定有多少个流程步骤
            if (Request.Headers.Host.IndexOf("192.168.0.20") == -1)
            {
                return ApiResult.Create(false, "功能正在完善");
            }
            return null;
        }//查

        /// <summary>
        /// 退回报销流程
        /// </summary>
        /// <param name="taskId">退回的报销ID</param>
        /// <returns></returns>
        public object RollBackTask(int taskId, string token)
        {
            return ApiResult.Create(false, "功能正在完善");
        }//删

    }
}
