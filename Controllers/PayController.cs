using System;
using System.Web.Http;

namespace TransportManage.Controllers
{
    using AppHelpers;
    using Filters;
    using Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http.Cors;
    using static TransportManage.StatisticsController;
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/Pay")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class PayController : ApiController
    {
        /// <summary>
        /// 查询司机订单
        /// </summary>
        /// <param name="name">司机名称</param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="workSite"></param>
        /// <param name="dump"></param>
        /// <param name="token"></param>
        /// <param name="vehiclePlateNumber"></param>
        /// <returns></returns>
        [HttpGet, Route("GetDriverTask")]
        public object GetDriverTask(string name, DateTime? startTime, DateTime? endTime, string workSite, string dump, string token, string vehiclePlateNumber, int taskstatus = 4)
        {
            var userInfo = new UserInfoData(Request);
            if (!userInfo.IsBoss)
            {
                return ApiResult.Create(false, "司机没有权限调用此借口");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                var r = (List<StatisticsDetail>)MethodHelp.FuzzySearching(c, new ShortcutData
                {
                    DriverName = name,
                    Dump = dump,
                    EndTime = endTime,
                    PlateNumber = vehiclePlateNumber,
                    StartTime = startTime,
                    TaskStatus = taskstatus,
                    Worksite = workSite
                }, companyId);
                //去除已经生成订单的TaskId
                var isPayTaskId = c.Select<int>(Sql.GetIsPayTaskIdSql);
                r = r.Where(d => !isPayTaskId.Contains(d.TaskId)).ToList();
                return ApiResult.Create(true, r.ConvertAll(d => new { d.Driver, TaskId = d.TaskId, Time = d.UnloadTime, d.TaskNumber, WorkSite = d.Name, d.SoilType, d.Dump, d.PlateNumber, d.DriverPrice }));
            }
        }

        /// <summary>
        /// 生成已审核订单的付款码
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("AddPayOrder")]
        public object AddPayOrder(List<int> taskId, string token)
        {
            var userInfo = new UserInfoData(Request);
            if (userInfo.IsAdmin || userInfo.IsBoss)
            {
                using (var c = AppDb.CreateConnection(TransportManage.Models.AppDb.TransportManager))
                {
                    //验证是否是满足同一性问题
                    MethodHelp.VerifyTask(taskId);
                    //获取付款单编号
                    var number = MethodHelp.GetOrderNumber(OrderNumberType.FK, c);
                    var employeeId = Convert.ToInt32(Request.Properties["Id"]);
                    MethodHelp.InsertPayOrder(taskId, c, number, employeeId);
                    return ApiResult.Create(true, "操作成功");
                }
            }
            else
                return ApiResult.Create(false, "没有权限调用此借口");
        }

        /// <summary>
        /// 取消付款单，确认付款单
        /// </summary>
        /// <param name="payOrderId">付款单</param>
        /// <param name="type">1为付款，2为退回</param>
        /// <returns></returns>
        [HttpPost, Route("UpdatePayOrder")]
        public object UpdatePayOrder(int payOrderId, int type, string token)
        {
            var _userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (type != 1 & type != 2)
                {
                    return ApiResult.Create(false, "不能识别操作指令");
                }
                if (type == 1)
                {

                    MethodHelp.ConfirmPayOrder(c, payOrderId);
                }
                if (type == 2)
                {
                    MethodHelp.RollBackPayOrder(c, payOrderId);
                }
                return ApiResult.Create(true, "操作成功");
            }
        }

        /// <summary>
        /// 查看付款单
        /// </summary>
        /// <param name="payOrderNum">订单码</param>
        /// <param name="name">司机名称</param>
        /// <param name="plateNumber">车牌号码</param>
        /// <returns></returns>
        [HttpGet, Route("GetPayOrder")]
        public object GetPayOrder(string token, string payOrderNum = null, string name = null, string plateNumber = null)
        {
            var userInfo = new UserInfoData(Request);
            if (userInfo.IsAdmin || userInfo.IsBoss)
            {
                using (var c = AppDb.CreateConnection(AppDb.TransportManager))
                {
                    return c.Select<object>(Sql.GetPayOrder, new { PayOrderNumber = "%" + payOrderNum + "%", Name = "%" + name + "%", PlateNumber = "%" + plateNumber + "%", DriverName = "%" + name + "%" });
                }
            }
            else
            {
                return ApiResult.Create(false, "没有权限调用此接口");
            }
        }

        /// <summary>
        /// 查看付款单明细
        /// </summary>
        /// <param name="payOrderId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetPayOrderDetail")]
        public object GetPayOrderDetail(int payOrderId, string token)
        {
            var userInfo = new UserInfoData(Request);
            if (userInfo.IsBoss || userInfo.IsAdmin)
            {
                using (var c = AppDb.CreateConnection(AppDb.TransportManager))
                {
                    var r = c.Select<PayOrderDetailData>(Sql.GetPayOrderDetail, new { payOrderId });
                    return ApiResult.Create(true, new { DriverName = r[0].DriverName, PlateNumber = r[0].PlateNumber, PayNumber = r[0].TaskNumber, Data = r.ConvertAll(d => { return new { CompleteTime = d.UnloadingTIme, PayNumber = d.PayNumber, Dump = d.UnloadingPos, WorkSite = d.LoadingPos, d.SoilType, d.PlateNumber, d.DriverPrice, d.ExtraCost }; }) });
                }

            }
            return ApiResult.Create(false, "没有权限调用此接口");
        }


        /// <summary>
        /// 查看付款单明细
        /// </summary>
        /// <param name="payOrderId">付款单码</param>
        /// <returns></returns>
        [HttpGet, Route("GetPayOrderSummary")]
        public object GetPayOrderSummary(int payOrderId, string token)
        {
            var userInfo = new UserInfoData(Request);
            if (userInfo.IsBoss || userInfo.IsAdmin)
            {
                using (var c = AppDb.CreateConnection(AppDb.TransportManager))
                {
                    var r = c.Select<PayOrderDetailData>(Sql.GetPayOrderSummary, new { payOrderId });
                    foreach (var item in r)
                    {
                        var a = new int[1, 5, 841, 8, 84];

                        var lt = item.LoadingTimeList.Split(',').ReverseSort();
                        item.LoadingTime = lt[lt.Length - 1];
                        var ut = item.UnloadingTimeList.Split(',').ReverseSort();
                        item.UnloadingTIme = ut[0];
                    }
                    return ApiResult.Create(true, new { DriverName = r[0].DriverName, PlateNumber = r[0].PlateNumber, r[0].PayNumber, Data = r });
                }
            }
            else
            {
                return ApiResult.Create(false, "没有权限调用此接口");
            }
        }
        class PayOrderDetailData
        {
            public string TaskNumber { get; set; }
            [JsonIgnore]
            public string PlateNumber { get; set; }
            [JsonIgnore]
            public string PayNumber { get; set; }

            public string LoadingPos { get; set; }
            public string UnloadingPos { get; set; }
            [JsonIgnore]
            public string DriverName { get; set; }
            public string SoilType { get; set; }
            [JsonIgnore]
            public int PayStatus { get; set; }
            [JsonIgnore]
            public int OrderDetailId { get; set; }
            public int DriverPrice { get; set; }
            public int ExtraCost { get; set; }
            public int Num { get; set; }
            [JsonIgnore]
            public string TaskIdList { get; set; }
            public string LoadingTime { get; set; }
            public string UnloadingTIme { get; set; }
            [JsonIgnore]
            public string LoadingTimeList { get; set; }
            [JsonIgnore]
            public string UnloadingTimeList { get; set; }
        }
    }
}
