using AppHelpers;
using System;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/Check")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class CheckController : ApiController
    {
        /// <summary>
        /// 审核司机已完成的任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns></returns>
        [HttpGet, Route("CheckTask")]
        public object CheckTask(int taskId, string token)
        {
            //验证操作人是否拥有审核该步骤的权限
            //记录任务订单完成情况
            //验证审核是否已经完成
            //执行审核流
            if (taskId == 0)
            {
                return ApiResult.Create(false, "参数不能为空");
            }
            var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
            var employeeId = Convert.ToInt32(Request.Properties["Id"]);
            var userInfo = new UserInfoData(Request);
            if (!userInfo.IsBoss)
            {

                MethodHelp.VerifyAuth(companyId, employeeId, taskId);
            }

            //审核任务，若任务已经进入审核流那么审核流加一，若任务还没进入审核流那么添加此任务进审核流。并且验证是否有权限审核该任务
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                MethodHelp.VerifyIsFinsh(taskId);
                return c.Update(Sql.CheckedTask, new { taskId, employeeId }) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");
            }
        }//增、改


        /// <summary>
        /// 获取正在审核流的订单
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public object GetFlowingTask(string token)
        {//如何确定有多少个流程步骤
            return ApiResult.Create(false, "功能正在完善");
        }//查

        /// <summary>
        /// 执行任务回滚到未进入审核流的状态
        /// </summary>
        /// <param name="taskId">被回滚的任务ID</param>
        /// <returns></returns>
        public object RollBackTask(int taskId)
        {
            if (Request.Headers.Host.IndexOf("192.168.0.20") == -1)
            {
                return ApiResult.Create(false, "功能正在完善");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                return c.Update(Sql.RollBackTask, new { taskId }) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");

            }
        }//删
    }
}
