using System;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/Replenish")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class ReplenishController : ApiController
    {

        /// <summary>
        /// 补录订单（仅允许Admin以及boss有权限操作此接口）
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("ReplenishmentOrder")]
        public object ReplenishmentOrder(ReplenishData data, string token)
        {
            var userInfo = new UserInfoData(Request);
            if (userInfo.IsAdmin || userInfo.IsBoss)
            {
                using (var c = AppDb.CreateConnection(AppDb.TransportManager))
                {
                    return ApiResult.Create(false, "此功能暂未开放");
                    //判断订单是否已经过期
                    //MethodHelp.VerifyOrderListIsExpires(c, data.OrderId);

                    ////验证OrderId与OrderDetailId 是否匹配
                    //MethodHelp.VerifyOrderIsMatchOrderDetailId(c, data.OrderDetailId, data.OrderId);

                    ////生成补录订单码
                    //var s = MethodHelp.GetOrderNumber(OrderNumberType.BD, c);
                    ////查找DriverId
                    //var i = MethodHelp.GetDriverCarId(c, data.Name, data.VehiclePlateNumber);
                    ////存储司机任务详细信息
                    //var ti = MethodHelp.InsertTask(c, new InsertTaskData { CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]), DriverCarId = i, OrderDetailId = data.OrderDetailId, TaskNumber = s, TaskStatus = 2 });

                    ////存储补录订单记录
                    //MethodHelp.InsertBDActionLog(c, ti, Convert.ToInt32(Request.Properties["Id"]), data.Remark);
                    //return ApiResult.Create(true, "操作成功");
                }
            }
            return ApiResult.Create(false, "没有相应的权限调用此接口");
        }

        public class ReplenishData
        {
            public string TaskNumber { get; set; }
            public int OrderId { get; set; }
            public int OrderDetailId { get; set; }
            public string Name { get; set; }
            public string VehiclePlateNumber { get; set; }
            public string Remark { get; set; }
        }
    }
}
