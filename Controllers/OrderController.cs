using AppHelpers;
using Dapper;
using Jetone.Geodesy;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{

    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/Order")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class OrderController : ApiController
    {
        /// <summary>
        /// 获取对应的用户的信息
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetUserInfo")]
        public object GetUserInfo(string token)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var employeeId = Convert.ToInt32(Request.Properties["Id"]);
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                if (userInfo.IsBoss || userInfo.IsAdmin)
                {//老板或管理员

                    //TODO 区分工地管理员跟超管 ，以便返回不同的数据集，超管增加是否过期，以及过期的车辆

                    IList<AdminController.DataList> worksiteIdList;
                    if (userInfo.IsBoss)
                    {
                        worksiteIdList = c.Select<AdminController.DataList>(Sql.GetAllWorksiteId, new { companyId });
                        return new
                        {
                            Status = true,
                            IsBoss = true,
                            IsAdmin = false,
                            IsDriver = false,
                            Result = c.Select<Info>(Sql.GetOrderCount, new { worksiteIdList = worksiteIdList.ConvertAll(d => d.Id), employeeId, companyId }).FirstOrDefault(),
                            ExpireAccount = c.SelectScalar<int>(Sql.GetExpireAccount, new { companyId })
                        };
                    }
                    else
                    {
                        worksiteIdList = c.Select<AdminController.DataList>(Sql.GetAdminWorksite, new { employeeId, id = companyId });
                        return new
                        {
                            Status = true,
                            IsAdmin = true,
                            IsDriver = false,
                            Result = c.Select<Info>(Sql.GetOrderCount, new { worksiteIdList = worksiteIdList.ConvertAll(d => d.Id), employeeId, companyId }).FirstOrDefault()
                        };
                    }

                }
                else if (!userInfo.IsBoss && !userInfo.IsAdmin)
                {//司机信息

                    return new
                    {
                        Status = true,
                        IsAdmin = false,
                        IsDriver = true,
                        Result = c.Select<DriverInfo>(Sql.GetDrive, new { driverId = employeeId }).FirstOrDefault()
                    };
                }
                else
                {
                    return ApiResult.Create(false, "没有权限调用此接口");
                }

            }
        }

        /// <summary>
        /// 获取司机正在进行中的任务
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetRuningTask")]
        public object GetRuningTask(string token)
        {

            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {

                var r = c.Select<RuningTask>(Sql.GetRuningTask, new { driverId = Convert.ToInt32(Request.Properties["Id"]), companyId = Convert.ToInt32(Request.Properties["CompanyId"]) }).FirstOrDefault();
                if (r == null)
                {//没有正在执行的任务，返回还未过期的Order 带有order的消纳场集合跟泥土类型集合
                    using (var gd = c.SetupCommand(Sql.GetOrder, new { companyId = Convert.ToInt32(Request.Properties["CompanyId"]), employeeId = Convert.ToInt32(Request.Properties["Id"]) }))
                    using (var reader = gd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                    {
                        var orderInfo = reader.ToEntities<OrderInfo>();
                        orderInfo.ForEach(d => d.SelectRout = new List<InfoData>());
                        reader.NextResult();
                        reader.JoinEntities(orderInfo, d => d.SelectRout, d => d.OrderId, "OrderId");
                        return ApiResult.Create(false, orderInfo);
                    }
                }
                var tempResult = c.Select<TaskD>(Sql.GetRunTaskDetail, new { taskId = r.Id });
                r.TaskDetail = new TaskDb();
                r.TaskDetail.ImageUrl = new List<string>();
                var imageUrl = new List<string>();

                foreach (var item in tempResult)
                {
                    r.TaskDetail.Time = item.Time;
                    r.TaskDetail.ImageUrl.Add(item.ImageUrl);
                }
                return ApiResult.Create(true, r);
            }
        }

        /// <summary>
        /// 查看订单列表
        /// </summary>
        /// <param name="status"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet, Route("GetTask")]
        public object GetTask(int status, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var db = c.Select<TaskInfo>(Sql.GetDriverFinishOrder2, new { taskStatus = status, driverId = Convert.ToInt32(Request.Properties["Id"]), companyId = Convert.ToInt32(Request.Properties["CompanyId"]) });
                var loadingTaskDetailIds = db.ConvertAll(i => i.LoadingTaskId);
                using (var cmd = c.SetupCommand(Sql.GetTaskDetailPhoto, new { taskDetailId = loadingTaskDetailIds }))
                using (var imgReader = cmd.ExecuteReader())
                {
                    imgReader.JoinEntities<TaskInfo, ImageData, int>(db, d => d.LoadingImages, m => m.LoadingTaskId, "TaskDetailId");
                }
                var unloadingTaskDetailIds = db.ConvertAll(i => i.UnloadingTaskId);
                using (var cmd = c.SetupCommand(Sql.GetTaskDetailPhoto, new { taskDetailId = unloadingTaskDetailIds }))
                using (var imgReader = cmd.ExecuteReader())
                {
                    imgReader.JoinEntities<TaskInfo, ImageData, int>(db, d => d.UnloadingImages, m => m.UnloadingTaskId, "TaskDetailId");
                }
                return ApiResult.Create(true, db);
            }
        }


        /// <summary>
        ///  获取有效的订单详情
        /// </summary>
        /// <param name="companyId">公司ID</param>
        /// <returns></returns>
        [HttpGet, Route("GetRunOrder")]
        public object GetRunOrder(int companyId, string token)
        {
            //工管登陆只允许查看工管自己下单的列表
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                string getOrderSql = Sql.GetOrder;
                if (userInfo.IsAdmin)
                {
                    getOrderSql = getOrderSql.Replace("condition", " AND o.Operater=@Operater");
                }
                using (var gd = c.SetupCommand(Sql.GetOrder, new { companyId }))
                using (var reader = gd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                {

                    var orderInfo = reader.ToEntities<OrderInfo>();
                    orderInfo.ForEach(d => d.SelectRout = new List<InfoData>());
                    reader.NextResult();
                    reader.JoinEntities(orderInfo, d => d.SelectRout, d => d.OrderId, "OrderId");
                    return ApiResult.Create(false, orderInfo);
                }
            }
        }

        /// <summary>
        ///获取司机正在执行的任务
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetDriverTask")]
        public object GetDriverTask(string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var r = c.Select<RuningTask>(Sql.GetRuningTask, new { driverId = Convert.ToInt32(Request.Properties["Id"]), companyId = Convert.ToInt32(Request.Properties["CompanyId"]) }).FirstOrDefault();
                r.TaskDetail = c.Select<TaskDb>(Sql.GetRunTaskDetail, new { taskId = r.Id }).GetFirst();
                return ApiResult.Create(true, r);
            }
        }

        /// <summary>
        /// 进行下一步流程
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("TaskNext")]
        public object TaskNext(TaskNextData data, string token)
        {
            if (data.DeliveryTaskId == 0)
            {
                return ApiResult.Create(false, "任务ID不能为空");
            }

            if (string.IsNullOrWhiteSpace(data.TaskStep))
            {
                return ApiResult.Create(false, "步骤不能为空");
            }
            if (data.OriginalUrl.Count == 0)
            {
                return ApiResult.Create(false, "图片不能为空");
            }
            if (data.lat == 0)
            {
                return ApiResult.Create(false, "未能获取经度");
            }
            if (data.lng == 0)
            {
                return ApiResult.Create(false, "未能获取维度");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var ts = c.Select<VerifyTaskData>(Sql.verifyTask, new { id = data.DeliveryTaskId }).GetFirst();
                if (ts.TaskStatus == 2)
                {
                    return ApiResult.Create(false, "该任务已完成");
                }
                if (ts.TaskStatus == 0 && data.TaskStep != "装货")
                {
                    return ApiResult.Create(false, "该任务处在装货阶段");
                }
                if (ts.TaskStatus == 1 && data.TaskStep != "卸货")
                {
                    return ApiResult.Create(false, "该任务处在卸货阶段");
                }
                if (ts.DueTime < System.DateTime.Now)
                {
                    return ApiResult.Create(false, "该任务已经过期");
                }
                ////接受图片保存服务器


                HttpWebRequest request = null;
                var requestList = new List<HttpWebRequest>();
                var filesNameList = new List<string>();
                if (data.OriginalUrl[0].Contains("http"))
                {
                    foreach (var item in data.OriginalUrl)
                    {
                        request = (HttpWebRequest)WebRequest.Create(item);
                        requestList.Add(request);
                    }
                }
                else
                {
                    var restclient = new RestClient("http://dev.jetone.cn:8098/zhatu/WxApi/api/WxJSSdk/GetAccessToken");
                    var restRequest = new RestRequest(Method.GET);
                    IRestResponse response = restclient.Execute(restRequest);
                    dynamic a = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    string accessToken = a["<access_token>k__BackingField"];
                    foreach (var item in data.OriginalUrl)
                    {
                        request = (HttpWebRequest)WebRequest.Create("http://file.api.weixin.qq.com/cgi-bin/media/get?access_token=" + accessToken + "&media_id=" + item);
                        requestList.Add(request);
                    }
                }
                data.ImageUrl = new List<string>();
                var tr = c.OpenTransaction();
                foreach (var item in requestList)
                {
                    ActionHelper.RetryIfFailed<WebException>(() =>
                    {

                        using (HttpWebResponse response = (HttpWebResponse)item.GetResponse())
                        using (Stream r = response.GetResponseStream())
                        using (Image img = Image.FromStream(r))
                        using (Graphics g = Graphics.FromImage(img))
                        using (SolidBrush brush = new SolidBrush(Color.Red))
                        using (Font f = new Font("Arial", 25))
                        {
                            var sysTime = System.DateTime.Now.ToString();
                            var astringSize = g.MeasureString(data.DdLocation, f);
                            var tstringSize = g.MeasureString(sysTime, f);
                            var ap = new PointF(img.Width - astringSize.Width, img.Height - astringSize.Height);
                            var tp = new PointF(img.Width - tstringSize.Width, img.Height - astringSize.Height - tstringSize.Height - 2);
                            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            g.DrawString(data.DdLocation, f, brush, ap);
                            g.DrawString(sysTime, f, brush, tp);
                            string newFileName = DateTime.Now.ToString("yyyyMMddHHmmss_ffff", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".jpeg";
                            img.Save(Path.Combine(HttpContext.Current.Server.MapPath("~/photos/"), newFileName));
                            data.ImageUrl.Add(newFileName);
                        }
                    }, 5, 1000)
                .Invoke();
                }
                //var result = c.SetupCommand(Sql.InsertTaskNext, data).Update();
                var photoList = new List<dynamic>();
                data.TaskStepAddress = data.DdLocation;
                var result = tr.SelectScalar<int>(Sql.InsertTaskNext, data);
                foreach (var item in data.ImageUrl)
                {
                    var tempInsertData = new { TaskDetailId = result, ImageUrl = item };
                    photoList.Add(tempInsertData);
                }
                var insertMultiplePhoto = c.Execute(Sql.InsertTaskMultiplePhoto, photoList, tr);

                //var result = c.SetupCommand(Sql.InsertTaskNext,data).Update();

                if (result == 0)
                {
                    return ApiResult.Create(false, "执行下一步失败");
                }
                //更新Task的状态为1
                if (data.TaskStep == "装货")
                {

                    c.Execute(Sql.UpdateLoadingTaskStatus, new { id = data.DeliveryTaskId, status = 1 }, tr);
                }
                if (data.TaskStep == "卸货")
                {
                    c.Execute(Sql.UpdateUnloadingTaskStatus, new { id = data.DeliveryTaskId, status = 2 }, tr);

                }
                tr.Commit();
                tr.Dispose();
                return ApiResult.Create(true, "执行下一步成功");

            }
        }

        /// <summary>
        /// 司机选择一个订单路线
        /// </summary>
        /// <param name="data"></param>
        /// <returns>新增任务的ID</returns>
        [HttpPost, Route("api/Order/ApplyTask")]
        public object ApplyTask(ApplyData data, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (data.OrderDetailId == 0)
                {
                    return ApiResult.Create(false, "OrderDetailId不能为空");
                }

                var driverId = Convert.ToInt32(Request.Properties["Id"]);
                //验证司机是否正在执行一个任务
                var driverTask = c.SelectScalar<GetDriverCar>(Sql.GetDriverStatus, new { driverId });
                if (driverTask != null)
                {
                    return ApiResult.Create(false, "不能重复执行任务");
                }
                //TODO 验证该订单路线是否是该公司所有



                //intsert task table with r;
                var driverCarId = c.SelectScalar<int>(Sql.GetDriverCar, new { driverId });
                var number = c.SelectScalar<string>(Sql.GetNumber, null);

                var price = c.SelectScalar<Price>(Sql.GetTaskPrice, new { data.OrderDetailId });

                //TODO插入前判断是否已经又正在执行的任务
                return c.SelectScalar<int>(Sql.AddTask, new { driverCarId, OrderDetailId = data.OrderDetailId, CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]), number, price.DumpPrice, price.WorkSitePrice, price.DriverPrice, price.ExtraPrice }) == 0 ? ApiResult.Create(false, "操作失败") : ApiResult.Create(true, "操作成功");
            }
        }
        private class Price
        {

            public int WorkSitePrice { get; set; }
            public int DriverPrice { get; set; }
            public int DumpPrice { get; set; }
            public int ExtraPrice { get; set; }
        }


        /// <summary>
        /// 取消任务，把任务状态改成3
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns></returns>
        [HttpGet, Route("CancelTask")]
        public object CancelTask(int taskId, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var driverId = Convert.ToInt32(Request.Properties["Id"]);
                var t = c.ExecuteScalar<string>(Sql.VerifyDriver, new { driverId, taskId });
                if (t == null)
                {
                    return ApiResult.Create(true, "该任务不属于司机");
                }
                var t2 = c.ExecuteScalar<int>(Sql.VerifyTaskstatus, new { taskId });
                if (t2 == 2)
                {
                    return ApiResult.Create(true, "该任务已经完成，不能取消");
                }
                if (t2 == 3)
                {
                    return ApiResult.Create(false, "该任务已经被取消");
                }
                var r = c.Execute(Sql.Cancel, new { taskId });
                if (r > 0)
                {
                    return ApiResult.Create(true, "取消任务成功");

                }
                return ApiResult.Create(false, "操作失败");
            }

        }


        [NonAction]
        private bool IsCompanyOrder(SqlConnection c, int companyId, int orderId)
        {
            var isTrue = c.Select<string>(Sql.GetCompanyOrder, new { companyId, orderId });
            if (isTrue == null)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// 新增配送方案
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("AddOrderDetail")]
        public object AddOrderDetail(AddOrderDetailData data, string token)
        {// TODO验证是否有权限给某个Order增加路线

            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                //TODO判断增加的方案订单是否属于该公司
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                var userId = Convert.ToInt32(Request.Properties["Id"]);
                if (!IsCompanyOrder(c, companyId, data.OrderId))
                {
                    return ApiResult.Create(false, "该订单不属于此公司");

                }
                using (var tr = c.OpenTransaction())
                {
                    if (userInfo.IsBoss || userInfo.IsAdmin)
                    {
                        if (data.DumpPrice <= 0)
                        {
                            return ApiResult.Create(false, "消纳场价格需大于零");
                        }
                        if (data.DriverPrice <= 0)
                        {
                            return ApiResult.Create(false, "司机价格需大于零");
                        }

                        //获取插入数据
                        var odata = c.Query<OrderInfoData>(Sql.GetOrderInfo, new { orderId = data.OrderId }, tr).First();
                        if ((data.DriverPrice + data.DumpPrice) >= odata.WorkPrice)
                        {
                            return ApiResult.Create(false, "司机价格、消纳场价格之和超过该工地价格");
                        }
                        //找出消纳场Id
                        data.Dump = data.Dump.Trim();
                        var dumpId = c.ExecuteScalar<int>(Sql.GetOrderDumpId, new { data.Dump, companyId }, tr);
                        if (dumpId == 0)
                        {
                            return ApiResult.Create(false, "未能找到该消纳场");
                        }
                        //找出泥土类型Id
                        data.SoilType = data.SoilType.Trim();
                        var soilTypeId = c.ExecuteScalar<int>(Sql.GetOrderSoilTypeId, new { data.SoilType, companyId }, tr);
                        if (soilTypeId == 0)
                        {
                            return ApiResult.Create(false, "未能找到该泥土类型");
                        }
                        var r = c.ExecuteScalar<int>(Sql.AddOrderDetailSql, new
                        {
                            KillMudstoneSiteId = dumpId,
                            KillSitePrice = data.DumpPrice,
                            WorkSiteId = odata.WorkSiteId,
                            WorkPrice = odata.WorkPrice,
                            SoilTypeId = soilTypeId,
                            BossPrice = odata.WorkPrice - data.DumpPrice - data.DriverPrice,
                            OrderId = data.OrderId,
                            DriverPrice = data.DriverPrice,
                            CompanyId = 2,
                            Operater = userId

                        }, tr);
                        if (r == 0)
                        {
                            return ApiResult.Create(true, "操作失败");
                        }
                        tr.Commit();
                        return ApiResult.Create(true, r);
                    }
                    else
                    {
                        return ApiResult.Create(false, "此借口只允许超管登陆或工管");
                    }
                }
            }
        }


        /// <summary>
        /// 工地订单开关
        /// </summary>
        /// <param name="orderStatus">1为开启，2为关闭</param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPost, Route("UpdateOrderStatus")]
        public object UpdateOrderStatus(int orderStatus, List<int> orderId, string token)
        {
            //TODO 验证需操作的OrderId是否有必要
            if (orderId.Count > 1)
            {
                return ApiResult.Create(false, "暂时只支持一个订单开关");
            }

            if (orderStatus > 2 || orderStatus < 1)
            {
                return ApiResult.Create(false, "接口参数错误");
            }

            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {

                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                if (!IsCompanyOrder(c, companyId, orderId.FirstOrDefault()))
                {
                    return ApiResult.Create(false, "该订单不是此公司所有");
                }
                if (userInfo.IsBoss || userInfo.IsAdmin)
                {
                    if (userInfo.IsAdmin)
                    {
                        var worksiteadminId = c.Query<int>(Sql.GetAdminOrder, new { companyId }).AsList();
                        if (!worksiteadminId.Contains(orderId[0]))
                        {
                            return ApiResult.Create(false, "不允许对该订单的操作");
                        }
                    }

                    var result = c.Execute(Sql.UpdateOrderStatus, new { orderStatus, orderId, EmployeeId = Request.Properties["Id"], companyId });
                    if (result == 0)
                    {
                        return ApiResult.Create(false, "操作失败");
                    }
                    return ApiResult.Create(true, "操作成功");
                }
                else
                {
                    return ApiResult.Create(false, "此借口只允许超管登陆或工管");
                }
            }
        }

        /// <summary>
        /// 工地订单路线开关
        /// </summary>
        /// <param name="OrderDetailStatus">1为开，2为关</param>
        /// <param name="orderDetailId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("UpdateOrderDetailStatus")]
        public object UpdateOrderDetailStatus(int OrderDetailStatus, List<int> orderDetailId, string token)
        {
            if (OrderDetailStatus > 2 || OrderDetailStatus < 1)
            {
                return ApiResult.Create(false, "接口参数错误");
            }
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var employeeId = Request.Properties["Id"];
                var companyId = Request.Properties["CompanyId"];
                if (userInfo.IsAdmin && !userInfo.IsBoss)
                {//工管更改状态
                 //TODO 验证路线状态是否需要更改
                    var result = c.ExecuteScalar<string>(Sql.UpdateOrderDumpStatus, new { updateDumpStatus = OrderDetailStatus, orderDetailId, employeeId, companyId });
                    if (result != "succeed")
                    {
                        return ApiResult.Create(false, "操作失败");
                    }
                    return ApiResult.Create(false, "操作成功");
                }
                else if (userInfo.IsBoss)
                {//老板更改状态
                    int result = 0;
                    if (OrderDetailStatus == 1)
                    {
                        result = c.Execute(Sql.UpdateOpenAdminOrderDetail, new { orderDetailId, employeeId, companyId });

                    }
                    else if (OrderDetailStatus == 2)
                    {
                        result = c.Execute(Sql.UpdateCloseAdminOrderDetail, new { orderDetailId, employeeId, companyId });
                    }
                    else
                    {
                        return ApiResult.Create(false, "指令异常");
                    }

                    if (result > 0)
                    {
                        return ApiResult.Create(true, "操作成功");
                    }
                    return ApiResult.Create(false, "操作失败");
                }
                else
                {
                    return ApiResult.Create(false, "没有相应的权限");
                }
            }
        }

        /// <summary>
        /// 获取已完成的订单
        /// </summary>
        /// <param name="token"></param>
        /// <param name="pageCount">一页多少行</param>
        /// <param name="pageNumber">第几页</param>
        /// <returns></returns>

        [HttpGet, Route("GetCompleteTask")]
        public object GetCompleteTask(int pageCount, int pageNumber, string token)
        {
            var userInfo = new UserInfoData(Request);
            var r = GetCompleteTaskCarryTime(userInfo, Convert.ToInt32(Request.Properties["CompanyId"]), Convert.ToInt32(Request.Properties["Id"]), Convert.ToDateTime("2000/1/1"), System.DateTime.Now.Date, pageCount, pageNumber);
            return ApiResult.Create(true, r.Data, r.Count.ToString());
        }

        /// <summary>
        /// 获取已完成的订单
        /// </summary>
        /// <param name="pageCount">一页多少行</param>
        /// <param name="pageNumber">第几页</param>
        /// <returns></returns>

        internal DetailData GetCompleteTaskCarryTime(UserInfoData userInfo, int companyId, int employeeId, DateTime? startTime, DateTime? endTime, int pageCount = 0, int pageNumber = 0, List<int> taskId = null)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {

                List<TaskInfo> db;

                if (userInfo.IsAdmin)
                {
                    var tr = MethodHelp.GetAdminWorksiteData(c, employeeId, companyId);
                    db = MethodHelp.GetOrderTask(c, companyId, employeeId, tr.ConvertAll(d => d.Id) as List<int>, taskId);
                }
                else if (userInfo.IsBoss)
                {
                    db = MethodHelp.GetOrderTask(c, companyId, taskId: taskId);
                }
                else if (!userInfo.IsAdmin && !userInfo.IsBoss)
                {
                    db = MethodHelp.GetOrderTask(c, companyId, employeeId, taskId: taskId);
                }

                else
                {
                    throw new NotImplementedException("未能识别账号类型，请验证Token是否正确");
                }
                //获取任务Id装卸货的详细信息，并返回
                return MethodHelp.GetCompleteTaskDetail(db, pageCount, pageNumber, c, startTime, endTime);

            }
        }


        /// <summary>
        /// 获取老板设置的地址
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetAddress")]
        public dynamic GetAddress(string token)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfo.IsBoss || userInfo.IsAdmin)
                {
                    var id = Request.Properties["CompanyId"];
                    return c.Query(Sql.GetAddress, new { companyId = id });
                }
                else
                {
                    return ApiResult.Create(false, "此借口只允许超管登陆或工管");
                }
            }
        }


        /// <summary>
        /// 老板新增收货卸货地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("AddAddress")]
        public dynamic AddAdress(AddressData data, string token)
        {

            if (data == null)
            {
                return ApiResult.Create(false, "服务器接收不到新增数据");
            }
            if (data.Address == null)
            {
                return ApiResult.Create(false, "地址不能为空");
            }
            if (data.Type == 0)
            {
                return ApiResult.Create(false, "请确认新增地址是收货或卸货");
            }
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfo.IsBoss || userInfo.IsAdmin)
                {
                    var a = new
                    {
                        Name = data.Address,
                        CompanyId = Request.Properties["CompanyId"],
                        Operater = Request.Properties["Id"]
                    };
                    int r = 0;
                    if (data.Type == 1)
                    {
                        r = c.ExecuteScalar<int>(Sql.WorkAddress, a);
                    }
                    if (data.Type == 2)
                    {
                        r = c.ExecuteScalar<int>(Sql.KillAddress, a);
                    }
                    if (r == 0)
                    {
                        return ApiResult.Create(false, "新增失败");
                    }
                    return ApiResult.Create(true, r);
                }
                else
                {
                    return ApiResult.Create(false, "此接口只允许超管或工管调用");
                }
            }
        }
        /// <summary>
        /// 删除收货或卸货地的地址 
        /// </summary>
        /// <param name="id">地址ID</param>
        /// <param nam="addressType">地址类型</param>
        /// <returns></returns>
        [HttpGet, Route("DelAddress")]
        public dynamic DelAddress(int id, int addressType, string token)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "此借口只允许超管调用");
                }
                else
                {
                    int r = 0;
                    if (addressType == 1)
                    {
                        r = c.Update(Sql.DelWorkAddress, new { id, companyId });
                    }
                    if (addressType == 2)
                    {
                        r = c.Update(Sql.DelKillAddress, new { id, companyId });
                    }
                    if (r == 0)
                    {
                        return ApiResult.Create(false, "删除失败");
                    }
                    return ApiResult.Create(true, "删除成功");
                }
            }
        }

        internal class DetailData
        {
            public List<TaskInfo> Data { get; set; }
            public int Count { get; set; }
        }
        public class PositionData : IPositional
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
        class Info
        {
            public string Name { get; set; }
            public int MounthCount { get; set; }
            public int DayCount { get; set; }
            public int TaskCount { get; set; }
            public string CreateTime { get; set; }
            public string DueTime { get; set; }
        }
        class OrderInfoData
        {
            public int KillMudstoneSiteId { get; set; }
            public int KillSitePrice { get; set; }
            public int WorkSiteId { get; set; }
            public int WorkPrice { get; set; }
            public int SoilTypeId { get; set; }

            public int BossPrice { get; set; }
            public int OrderId { get; set; }
            public int DriverPrice { get; set; }
        }
        public class AddOrderDetailData
        {
            public int OrderId { get; set; }
            public string Dump { get; set; }
            public int DumpPrice { get; set; }
            public string SoilType { get; set; }
            public int DriverPrice { get; set; }
        }
        public class VerifyTaskData
        {
            public int TaskStatus { get; set; }
            public DateTime DueTime { get; set; }
        }
        internal class OrderInfo
        {
            public string OrderName { get; set; }
            public int OrderId { get; set; }
            public DateTime CreateTime { get; set; }
            public string WorkSite { get; set; }
            public double WorkPrice { get; set; }
            public int WorkSiteId { get; set; }
            public List<InfoData> SelectRout { get; set; }
        }
        internal class TaskInfo
        {
            public int TaskId { get; set; }
            public string SoilType { get; set; }
            public string TaskNumber { get; set; }
            public string DriverName { get; set; }
            public int DriverPrice { get; set; }
            public string PlateNumber { get; set; }
            public string TaskStep { get; set; }
            [JsonIgnore]
            public int DumpPrice { get; set; }
            [JsonIgnore]
            public int WorkPrice { get; set; }
            public int LoadingTaskId { get; set; }
            //[JsonConverter(typeof(Utilities.ChinaDateTimeConverter))]
            public DateTime CreateTime { get; set; }
            public DateTime CompleteTime { get; set; }
            public DateTime LoadingTime { get; set; }
            public string UnloadingPos { get; set; }
            public string LoadingPos { get; set; }
            public string LoadingMemo { get; set; }
            public int UnloadingTaskId { get; set; }
            public DateTime UnloadingTime { get; set; }
            public string UnloadingMemo { get; set; }
            public string LoadingAddress { get; set; }
            public string UnloadingAddress { get; set; }

            [JsonIgnore]
            public List<ImageData> LoadingImages { get; set; } = new List<ImageData>();
            [JsonIgnore]
            public List<ImageData> UnloadingImages { get; set; } = new List<ImageData>();

            public List<string> LoadingImageUrls { get { return LoadingImages.ConvertAll(i => i.ImageUrl); } }
            public List<string> UnloadingImageUrls { get { return UnloadingImages.ConvertAll(i => i.ImageUrl); } }
            public int TaskStatus { get; set; }
            [JsonIgnore]
            public int ExtraCost { get; set; }
        }

        internal class InfoData
        {
            [JsonIgnore]
            public int OrderId { get; set; }
            public int OrderDetailId { get; set; }
            public int SoilId { get; set; }
            public string SoilName { get; set; }
            public int KillSiteId { get; set; }
            public string KillSiteName { get; set; }
            public string DriverPrice { get; set; }

        }
        class DeliveryTask
        {
            public int Id { get; set; }
            public int FlowId { get; set; }
            public int Status { get; set; }
            public dynamic TaskDetail { get; set; }

        }

        class DriverDetail
        {
            public string TaskNumber { get; set; }
            public string DriverName { get; set; }
            public string PlateNumber { get; set; }
            public string TaskStep { get; set; }
            public string ImageUrl { get; set; }
            public int TaskId { get; set; }
            public string Pos { get; set; }
        }
        public class TaskNextData
        {
            public int DeliveryTaskId { get; set; }
            public double lat { get; set; }
            public double lng { get; set; }
            [JsonIgnore]
            public List<string> ImageUrl { get; set; }
            [JsonIgnore]
            [ConstrainLength(150, Action = ConstraintAction.ThrowException, Trim = true)]
            public string TaskStepAddress { get; set; }
            [ConstrainLength(10, Action = ConstraintAction.ThrowException, Trim = true)]
            public string TaskStep { get; set; }
            [ConstrainLength(50, Action = ConstraintAction.ThrowException, Trim = true)]
            public string Marker { get; set; }
            public string ImageBase { get; set; }

            public List<string> OriginalUrl { get; set; }

            public string DdLocation { get; set; }
        }

        /// <summary>
        /// 司机新增任务数据
        /// </summary>
        public class ApplyData
        {
            public int OrderDetailId { get; set; }
            public int OrderId { get; set; }
        }
        class finishDetail
        {
            public string ImageUrl { get; set; }
            public string TaskStep { get; set; }
            public DateTime CreateDate { get; set; }
            public string Marker { get; set; }
            public int TaskId { get; set; }
            public string Pos { get; set; }
        }

        class DriverInfo
        {
            public int CompanyId { get; set; }
            public string DriverName { get; set; }
            public string Cph { get; set; }
            public int MounthCount { get; set; }
            public int DayCount { get; set; }
        }
        class RuningTask
        {
            public string LoadingAddress { get; set; }
            public string UnloadingAddress { get; set; }
            public string TaskNumber { get; set; }
            public int DriverPrice { get; set; }
            public int Id { get; set; }
            public string TaskStatus { get; set; }
            public string WorkSite { get; set; }
            public string KillSite { get; set; }
            public int DumpPrice { get; set; }
            public string SoilType { get; set; }
            public TaskDb TaskDetail { get; set; }
        }
        class TaskD
        {
            public string ImageUrl { get; set; }
            public DateTime Time { get; set; }
        }
        class TaskDb
        {
            public List<string> ImageUrl { get; set; }
            public DateTime Time { get; set; }

        }
        class GetDriverCar
        {
            public int DriverCar { get; set; }
            public int TaskStatus { get; set; }
        }
        public class DeliveryTaskDetail
        {
            public int DeliveryTaskId { get; set; }
            public int Pos { get; set; }
            public decimal lat { get; set; }
            public decimal lng { get; set; }
            public string ImageUrl { get; set; }
            public string TaskStep { get; set; }
            public string Marker { get; set; }
            public string SoilType { get; set; }
            public string Url { get; set; }
        }

        class DriverFlow
        {
            public int FlowId { get; set; }
            public int DriverId { get; set; }
            public int VehicleId { get; set; }
            public int OrderId { get; set; }
            public int Status { get; set; }
            public DateTime AccepTime { get; set; }
            public DateTime CreateTime { get; set; }
            public DInfo DriverInfo { get; set; }
            public string ShippingPos { get; set; }
            public string LandingPos { get; set; }
        }
        class DInfo
        {
            public string Name { get; set; }
            public string Cph { get; set; }
            public string Tel { get; set; }
            public int Status { get; set; }
            public string Company { get; set; }
            public string AvatarURL { get; set; }
            public int MounthCount { get; set; }
            public int DayCount { get; set; }
            public int TaskCount { get; set; }
            public dynamic Address { get; set; }
        }
        public class AddressData
        {
            public string Address { get; set; }
            public int Type { get; set; }
        }
        class Detail
        {
            public string DriverName { get; set; }
            public string DriverTel { get; set; }
            public string PlateNumber { get; set; }
            public int FlowId { get; set; }
            public int TaskStatus { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public dynamic ImageUrl { get; set; }
            public dynamic Pos { get; set; }
            public int TaskId { get; set; }
            public string TaskStep { get; set; }
            public DateTime TaskDetailDate { get; set; }
        }



        class AddressDict
        {
            public int posId { get; set; }
            public dynamic Pos { get; set; }
            public int TaskId { get; set; }
            public string DriverName { get; set; }
            public string PlateNumber { get; set; }
            public DateTime CreateDate { get; set; }
            public dynamic ImageUrl { get; set; }
            public string TaskStep { get; set; }
        }
    }
    public class ImageData
    {
        public string ImageUrl { get; set; }
    }
}
