using AppHelpers;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using TransportManage.Models;
using static TransportManage.Controllers.AdminController;
using static TransportManage.Controllers.OrderController;
using static TransportManage.StatisticsController;

namespace TransportManage
{
    internal static class MethodHelp
    {
        internal static void VerifyIsFinsh(int taskId)
        {

            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var r = c.SelectScalar<string>(Sql.VerifyIsFinsh, new { taskId }) == null ?false : true;
                if (r)
                {
                    throw new Exception("任务已经审核完成");
                }
            }
        }

        internal static void VerifyAuth(int companyId, int employeeId, int taskId)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                //wojiushiyixikkkka
                var r = c.SelectScalar<string>(Sql.GetVerifyAuth, new { companyId, employeeId, taskId });
                if (r == null)
                {
                    throw new Exception("没有权限审核该步骤");
                }

            }
        }
        /// <summary>
        /// 验证订单与订单详情是否匹配
        /// </summary>
        /// <param name="c"></param>
        /// <param name="orderDetail"></param>
        /// <param name="orderId"></param>
        internal static void VerifyOrderIsMatchOrderDetailId(SqlConnection c, int orderDetail, int orderId)
        {
            if (c.SelectScalar<int>(Sql.VerifyOrderDetailSql, new { OrderId = orderId, OrderDetailId = orderDetail }) == 0)
            {
                throw new NotImplementedException("OrderDetailId与OrderId不匹配");
            }

        }

        /// <summary>
        /// 获取制定类型的订单吗
        /// </summary>
        /// <param name="type"></param>
        /// <param name="c"></param>
        /// <returns>字符串订单码</returns>
        internal static string GetOrderNumber(OrderNumberType type, SqlConnection c)
        {
            string sql = null;
            switch (type)
            {
                case OrderNumberType.ZT: sql = Sql.GetNumber; break;
                case OrderNumberType.BD: sql = Sql.GetBDNumber; break;
                case OrderNumberType.FK: sql = Sql.GetFKNumber; break;
                default:
                    break; throw new System.Exception("订单码类型指令异常");
            }
            var r = c.SelectScalar<string>(sql, null);
            if (r == null)
            {
                throw new NotImplementedException("生成唯一码失败");
            }
            return r;

        }

        /// <summary>
        /// 获取司机DriverCarId
        /// </summary>
        /// <param name="c">数据库连接</param>
        /// <returns>DriverCarId</returns>
        internal static int GetDriverCarId(SqlConnection c, string driverName, string driverPlateNumber)
        {
            if (driverName == null)
            {
                throw new ArgumentException("获取司机DriverCarId,参数driverName不能为空");
            }
            if (driverPlateNumber == null)
            {
                throw new ArgumentException("获取司机DriverCarId,参数driverPlateNumber不能为空");
            }
            var r = c.Select<int>(Sql.GetDriverCarIdSql, new { DriverName = driverName, PlateNumber = driverPlateNumber });
            if (r.Count > 1 || r.Count == 0)
            {
                throw new NotImplementedException("找不到相应的司机信息或司机不唯一");
            }
            return r[0];
        }

        /// <summary>
        /// 存储司机领取任务的详细信息
        /// </summary>
        /// <param name="c"></param>
        /// <param name="data"></param>
        /// <returns>返回为1则正常插入，0为异常插入</returns>
        internal static int InsertTask(SqlConnection c, InsertTaskData data)
        {
            if (data == null)
            {
                throw new ArgumentException("存储司机任务详情,参数InsertTaskData不能为空");
            }
            if (data.CompanyId == 0)
            {
                throw new ArgumentException("存储司机任务详情,参数InsertTaskData.CompanyId不能为空");
            }
            if (data.DriverCarId == 0)
            {
                throw new ArgumentException("存储司机任务详情,参数InsertTaskData.DriverCarId不能为空");
            }
            if (data.OrderDetailId == 0)
            {
                throw new ArgumentException("存储司机任务详情,参数InsertTaskData.OrderDetailId不能为空");
            }
            if (data.TaskNumber == null)
            {
                throw new ArgumentException("存储司机任务详情,参数InsertTaskData.TaskNumber不能为空");
            }
            if (data.TaskStatus == 0)
            {
                throw new ArgumentException("存储司机任务详情,参数InsertTaskData.TaskStatus不能为空");
            }

            //往Task表插入数据
            var i = c.SelectScalar<int>(Sql.InsertTask, data);
            if (i == 0)
            {
                throw new NotImplementedException("存储司机领取任务的详细信息失败");
            }
            return i;
        }

        /// <summary>
        /// 筛选出员工Id和任务Id不匹配的Id
        /// </summary>
        /// <param name="taskId">任务Id集合</param>
        /// <returns>返回布尔值</returns>
        internal static void VerifyTask(List<int> taskId)
        {
            if (taskId == null)
            {
                throw new ArgumentException("TaskId集合不能为空");
            }
            if (taskId.Count == 0)
            {
                throw new ArgumentException("TaskId集合至少有一项");
            }

            using (var c = AppDb.CreateConnection(TransportManage.Models.AppDb.TransportManager))
            {
                //验证传入数据集是否是同一个人，不执行差异个体
                if (c.Select<int>(Sql.VerifyTask, new { taskId }).Count != 1)
                {
                    throw new NotImplementedException("请检查任务集合是否都属于同一个司机或任务Id是否已经被审核");
                }
            }
        }

        /// <summary>
        /// 存储付款单信息
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="c"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        internal static void InsertPayOrder(List<int> taskId, SqlConnection c, string number, int operatorId)
        {
            if (taskId == null)
            {
                throw new ArgumentException("TaskId集合不能为空");
            }
            if (taskId.Count == 0)
            {
                throw new ArgumentException("TaskId集合至少有一项");
            }
            if (number == null)
            {
                throw new ArgumentException("number付款码不能为空");
            }
            using (var tr = c.OpenTransaction())
            {

                var payNumberId = tr.SelectScalar<int>(Sql.InsertPay, new { PayNumber = number, Operator = operatorId });
                if (payNumberId == 0)
                {
                    throw new NotImplementedException("存储付款单码失败");
                }

                if (tr.Update(Sql.InsertPayDetail, new { PayId = payNumberId }, taskId.ConvertAll(d => new { DeliveryTaskId = d })) != taskId.Count)
                {
                    throw new NotImplementedException("存储付款单失败");
                }
                tr.Commit();
            }

        }

        /// <summary>
        /// 确认付款单
        /// </summary>
        /// <param name="c"></param>
        /// <param name="payOrderId"></param>
        internal static void ConfirmPayOrder(SqlConnection c, int payOrderId)
        {
            //TODO 修改数据表，逻辑需重新编写
            if (payOrderId == 0)
            {
                throw new ArgumentException("确认订单操作payOrderNumber参数不能为空");
            }
            if (c.Update(Sql.OperatePayOrderSql, new { payOrderId, status = 1 }) == 0)
            {
                throw new NotImplementedException("付款操作失败");
            }
        }

        /// <summary>
        /// 取消付款
        /// </summary>
        /// <param name="c"></param>
        /// <param name="payOrderId">付款码</param>
        internal static void RollBackPayOrder(SqlConnection c, int payOrderId)
        {
            if (payOrderId == 0)
            {
                throw new ArgumentException("取消订单操作payOrderNumber参数不能为空");
            }
            //VerifyOrderNumberIsConfirm(c, payOrderNumber);
            using (var tr = c.OpenTransaction())
            {
                if (!(tr.Update(Sql.OperatePayOrderSql, new { payOrderId, status = 2 }) > 0))
                {
                    throw new NotImplementedException("取消付款失败");
                }
                if (tr.Update(Sql.InsertCancelPayData, new { payOrderId }) == 0)
                {
                    throw new NotImplementedException("存储取消单数据失败");
                }
                if (tr.Update(Sql.DeletPaySql, new { payOrderId }) == 0)
                {
                    throw new NotImplementedException("删除付款记录失败");
                }
                tr.Commit();
            }

        }

        /// <summary>
        /// 验证付款单是否已经被确认
        /// </summary>
        /// <param name="c"></param>
        /// <param name="payOrderNumber">付款码</param>
        internal static void VerifyOrderNumberIsConfirm(SqlConnection c, string payOrderNumber)
        {
            //查询数据库确认是否已经被处理过的订单码
        }


        /// <summary>
        /// 判断订单集合是否存在过期，存在即返回过期的Id
        /// </summary>
        /// <param name="c"></param>
        /// <param name="orderId">订单Id集合</param>
        /// <returns></returns>
        internal static List<int> VerifyOrderListIsExpires(SqlConnection c, List<int> orderId)
        {

            return null;
        }


        /// <summary>
        /// 判断订单是否已经过期
        /// </summary>
        /// <param name="c"></param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        internal static void VerifyOrderListIsExpires(SqlConnection c, int orderId)
        {
            if (orderId == 0)
            {
                throw new ArgumentNullException("判断订单是否过期不能缺失订单Id");
            }
            if (c.SelectScalar<int>(Sql.VerifyOrderIsExpiresSql, new { orderId }) == 2)
            {
                throw new NotImplementedException("订单已经过期");
            }
        }

        /// <summary>
        /// 存储补录订单日志
        /// </summary>
        /// <param name="c"></param>
        /// <param name="taskId"></param>
        /// <param name="operatorId"></param>
        internal static void InsertBDActionLog(SqlConnection c, int taskId, int operatorId, string remark)
        {
            if (taskId == 0)
            {
                throw new ArgumentNullException("存储补录订单日志,taskId参数不能为空");
            }
            if (operatorId == 0)
            {
                throw new ArgumentNullException("存储补录订单日志,operatorId参数不能为空");
            }
            if (remark == null)
            {
                throw new ArgumentNullException("存储补录订单日志,remark参数不能为空");
            }
            if (c.Update(Sql.InsertBDActionLog, new { TaskId = taskId, OperatorId = operatorId, Remark = remark }) == 0)
            {
                throw new NotImplementedException("存储补单日志失败");
            }
        }

        /// <summary>
        /// 模糊查询
        /// </summary>
        /// <param name="c"></param>
        /// <param name="data">查询条件</param>
        /// <returns></returns>
        internal static List<StatisticsController.StatisticsDetail> FuzzySearching(SqlConnection c, ShortcutData data, int companyId)
        {

            //TODO 重发方法体，需要抽析；
            if (data == null)
            {
                throw new ArgumentNullException("模糊查询Data不能为空");
            }
            if ((data.DriverName ?? data.Dump ?? data.PlateNumber ?? data.Worksite) == null && (data.StartTime ?? data.EndTime) == null && data.TaskStatus == 0)
            {
                throw new ArgumentNullException("参数不能全为空");
            }

            data.Worksite = "%" + data.Worksite + "%";
            data.Dump = "%" + data.Dump + "%";
            data.DriverName = "%" + data.DriverName + "%";
            data.PlateNumber = "%" + data.PlateNumber + "%";
            var ss = data.StartTime ?? new DateTime(2000, 1, 1);
            var ee = (data.EndTime ?? System.DateTime.Now).Date.AddDays(1).AddSeconds(-1);
            var r = c.Select<StatisticsController.StatisticsDetail>(Sql.GetShortcutDetailSql, new { StartTime = data.StartTime ?? Convert.ToDateTime("2000/1/1"), data.PlateNumber, EndTime = (data.EndTime ?? System.DateTime.Now).Date.AddDays(1).AddSeconds(-1), data.DriverName, data.Dump, data.Worksite, data.TaskStatus, CompanyId = companyId });

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
            return r as List<StatisticsController.StatisticsDetail>;
        }

        /// <summary>
        /// 获取工地管理员工地信息
        /// </summary>
        /// <param name="c"></param>
        /// <param name="employeeId"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        internal static IList<DataList> GetAdminWorksiteData(SqlConnection c, int employeeId, int companyId)
        {
            return c.Select<DataList>(Sql.GetAdminWorksite, new { employeeId, Id = companyId });
        }

        /// <summary>
        ///获取每个完成订单的详细，并封装到每个任务里面
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        internal static DetailData GetCompleteTaskDetail(List<TaskInfo> db, int pageCount, int pageNumber, DbConnection c, DateTime? startTime = null, DateTime? endTime = null)
        {
            List<TaskInfo> dbtemp = null;
            if (pageCount == 0 & pageNumber == 0)
            {
                dbtemp = db;
            }
            else
            {
                dbtemp = Utilities.PagingMethod.Paging(db, pageCount, pageNumber).ToList<TaskInfo>();
            }
            var r = new DetailData();
            r.Count = db.Count();
            var temp = dbtemp.Where(d =>
            {
                return d.LoadingTaskId != 0;
            });
            var loadingTaskDetailIds = temp.ConvertAll(dd => dd.LoadingTaskId);
            using (var cmd = c.SetupCommand(Sql.GetTaskDetailPhoto, new { taskDetailId = loadingTaskDetailIds }))
            using (var imgReader = cmd.ExecuteReader())
            {
                imgReader.JoinEntities(temp, d => d.LoadingImages, m => m.LoadingTaskId, "TaskDetailId");
            }
            var unloadingTaskDetailIds = temp.ConvertAll(i => i.UnloadingTaskId);
            using (var cmd = c.SetupCommand(Sql.GetTaskDetailPhoto, new { taskDetailId = unloadingTaskDetailIds }))
            using (var imgReader = cmd.ExecuteReader())
            {
                imgReader.JoinEntities<TaskInfo, Controllers.ImageData, int>(temp, d => d.UnloadingImages, m => m.UnloadingTaskId, "TaskDetailId");
            }
            r.Data = dbtemp.Where(d =>
            {
                if (d.UnloadingTime < endTime.Value.AddDays(1).AddMinutes(-1) && d.UnloadingTime > startTime)
                {
                    return true;
                }
                else if (d.TaskNumber.IndexOf("BD") >= 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
                ).ToList<TaskInfo>();

            return r;

        }



        /// <summary>
        /// 获取订单任务
        /// </summary>
        /// <param name="c"></param>
        /// <param name="companyId"></param>
        /// <param name="employeeId"></param>
        /// <param name="worksiteIdList"></param>
        /// <returns></returns>
        internal static List<TaskInfo> GetOrderTask(SqlConnection c, int companyId, int employeeId = 0, List<int> worksiteIdList = null, List<int> taskId = null)
        {
            var maxNumber = c.SelectScalar<int>(Sql.GetCheckedFlowMaxNumber, new { });
            IList<TaskInfo> d;
            if (companyId == 0)
            {
                throw new ArgumentNullException("获取订单任务信息公司ID不能为空");
            }
            else if (employeeId != 0 & worksiteIdList == null)
            {
                //司机个人详情

                d = c.Select<TaskInfo>(Sql.GetDriverFinishOrder2.Replace("/*condition*/", @" OR f.FlowNumber=@conditionStatus"), new { DriverId = employeeId, flowNumber = 2, conditionStatus = maxNumber, companyId });
            }

            else if (employeeId != 0 & worksiteIdList != null)
            {
                //工地管理员详情
                d = c.Select<TaskInfo>(Sql.GetFinishOrder2.Replace("/*condition*/", @"OR f.FlowNumber=@conditionStatus").Replace("/*andCondition*/", @" AND od.WorkSiteId IN (@worksiteId)"), new { CompanyId = companyId, FlowNumber = 0, conditionStatus =maxNumber, Operater = employeeId, worksiteId = worksiteIdList });
            }
            else
            {
                //公司详情
                d = c.Select<TaskInfo>(Sql.GetFinishOrder2, new { CompanyId = companyId, FlowNumber= 0 });
            }
            if (taskId != null)
            {
                var temp = d.Where(s => taskId.Contains(s.TaskId)).ToList();
                return temp;
            }
            return d as List<TaskInfo>;
        }

    }

    public class InsertTaskData
    {
        public int TaskStatus { get; set; }
        public int OrderDetailId { get; set; }
        public int DriverCarId { get; set; }
        public int CompanyId { get; set; }
        public string TaskNumber { get; set; }
    }
    public enum OrderNumberType
    {
        ZT,//渣土车
        BD,//补单
        FK//付款单

    }
}