using AppHelpers;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;
using TransportManage.Utilities;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/Department")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class DepartmentController : ApiController
    {

        /// <summary>
        /// 新增子部门
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("AddDept")]
        public object AddDept(AddDeptData data, string token)
        {

            var userInfo = new UserInfoData(Request); ;
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                data.CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "只允许超管调用此接口");
                }
                if (c.Update(Sql.AddDept, data) > 0)
                {
                    return ApiResult.Create(true, "操作成功");
                }
                return ApiResult.Create(false, "操作失败");
            }
        }

        /// <summary>
        ///  删除部门(并不删除特定得部门)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet, Route("DelDept")]
        public object DelDept(int id, string token)
        {
            var userInfo = new UserInfoData(Request); ;
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "只允许超管调用此接口");
                }
                //只能删除没有人的部门
                if (c.SelectScalar<string>(Sql.GetDepartmentEmployee, new { id }) != null)
                {
                    return ApiResult.Create(false, "此部门不允许删除");
                }
                if (c.Update(Sql.DelDept, new { id }) > 0)
                {
                    return ApiResult.Create(true, "操作成功");
                }
                return ApiResult.Create(false, "操作失败");
            }
        }


        /// <summary>
        /// 更新部门
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("UpdateDepartment")]
        public object UpdateDepartment(UpdateDepartmentData data, string token)
        {

            var userInfo = new UserInfoData(Request); ;
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            using (var tran = c.OpenTransaction())
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "只允许超管调用此接口");
                }
                var list = new List<DepartmentData>();
                foreach (var item in data.EmployeeId)
                {
                    foreach (var ditem in data.DepartmentId)
                    {
                        list.Add(new DepartmentData() { DepartmentId = ditem, EmployeeId = item });
                    }
                }


                //删除员工所有部门
                var d = tran.Update(Sql.DelEmployDepartment, new { id = data.EmployeeId });

                //插入员工现有的部门
                var i = c.Execute(Sql.InsertEmployeeDepartment, list, tran);


                ////判断司机在原先部门是否有运输权限
                //var beforeIsAuthority = tran.SelectScalar<int>(@"SELECT COUNT(*) FROM TransportManage..EmployeeLevel WHERE EmployeeId=@employeeId AND Name = '司机'", new { employeeId = data.EmployeeId });
                ////判断司机更新后的部门是否有运输权限
                //var NowDepartmentAuthority = tran.SelectScalar<int>(Sql.IsDriverDepartment, new { deptId = data.DepartmentId });
                //if (beforeIsAuthority == 0 && NowDepartmentAuthority != 0)
                //{
                //    if (string.IsNullOrWhiteSpace(data.PlateNumber))
                //    {//判断车牌号是否为空
                //        return ApiResult.Create(false, "从没有权限运输的部门转到运输部门，需增加车牌");
                //    }
                //    //TODO绑定车辆与员工的信息
                //    var r = tran.Update(Sql.InsertDriverCarNewEmployee, new { data.PlateNumber, CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]), deptId = data.DepartmentId, data.EmployeeId });
                //    if (d > 0 & i > 0 & r > 3*data.EmployeeId.Count)
                //    {
                //        tran.Commit();
                //        return ApiResult.Create(true, "增加司机成功");
                //    }
                //}
                //if (beforeIsAuthority != 0 && NowDepartmentAuthority == 0)
                //{//TODO从有运输权限调动到没有权限部门，执行删除权限操作
                //    var de = tran.Update(Sql.DeleteEmployeeLevel, new { data.EmployeeId });

                //}
                if (d > 0 && i > 0)
                {
                    tran.Commit();
                    return ApiResult.Create(true, "操作成功");

                }
                return ApiResult.Create(false, "操作失败");
            }

            //SqlConnection connection = null;
            //SqlTransaction transaction = null;
            //connection = AppDb.CreateConnection(AppDb.TransportManager);
            //transaction = connection.OpenTransaction();
            //....
            //transaction.Dispose();

        }

        /// <summary>
        /// 获取子部门
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet, Route("GetSubDept")]
        public object GetSubDept(int id, string token)
        {
            var userInfo = new UserInfoData(Request); ;
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "只允许超管调用此接口");
                }
                return c.SelectJson(Sql.GetSubDept, new { id }).CreateJsonMessage<string>();
            }
        }

        /// <summary>
        /// 新增员工（车牌可空）
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("AddEmployee")]
        public object AddEmployee(AddEmployeeData data, string token)
        {
            //return ApiResult.Create(false, "功能正在完善");
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            using (var tr = c.OpenTransaction())
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "只允许超管调用此接口");
                }
                data.CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]);

                //插入新员工
                //插入员工部门
                var employeeId = tr.SelectScalar<int>(Sql.Insertemployee, new { data.Name, data.Tel, data.Password, data.CompanyId, data.deptId });

                if (!string.IsNullOrWhiteSpace(data.PlateNumber))
                {//车牌号码不为空时
                    //绑定员工车辆信息
                    var ic = tr.Update(Sql.InsertEmployeeCar, new { CompanyId = data.CompanyId, PlateNumber = data.PlateNumber, EmployeeId = employeeId });
                    if (ic == 2 & employeeId > 0)
                    {
                        tr.Commit();
                        return ApiResult.Create(true, "录入员工车牌、基本信息成功");
                    }
                    return ApiResult.Create(false, "操作失败");
                }
                if (employeeId > 0)
                {
                    tr.Commit();
                    return ApiResult.Create(true, "增加员工成功");
                }
                return ApiResult.Create(false, "增加员工失败");
            }
        }

        /// <summary>
        /// 删除员工（停用）
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("DelEmployee")]
        public object DelEmployee(int id, string token)
        {

            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "只允许超管调用此接口");
                }
                if (c.Execute(Sql.DelEmployee, new { id }) > 0)
                {
                    return ApiResult.Create(true, "操作成功");
                }
                return ApiResult.Create(false, "操作失败");

            }
        }

        /// <summary>
        ///  获取员工列表
        /// </summary>
        /// <param name="id">部门Id</param>
        /// <returns></returns>
        [HttpGet, Route("getEmployeeList")]
        public object GetEmployeeList(int id, string token)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                if (userInfo == null)
                {
                    return ApiResult.Create(false, "userInfo为空");

                }
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "只允许超管调用此接口");
                }
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);



                if (id == 0)
                {
                    id = c.SelectScalar<int>(Sql.GetDepartmentId, new { companyId });

                }
                return new
                {
                    ChildrenDept = c.Select<object>(Sql.GetDepartMentList, new { Id = id, CompanyId = companyId }),
                    Employee = c.Select<object>(Sql.GetEmployee, new { DepartmentId = id, CompanyId = companyId }),
                    CompanyName = c.Select<string>(Sql.GetCompanyName, new { companyId = companyId })
                };
            }
        }

        /// <summary>
        /// 修改人员信息（姓名，电话，密码，车牌信息）（车牌可空）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("UpdateEmployee")]
        public object UpdateEmployee(UpdateEmployeeData data, string token)
        {
            var userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            using (var tr = c.OpenTransaction())
            {
                if (!userInfo.IsBoss)
                {
                    return ApiResult.Create(false, "只允许超管调用此接口");
                }
                var u = tr.Update(Sql.UpdateEmployee, data);
                //找出人员的车辆，若无车辆则插入车辆，若有则修改车辆
                if (!string.IsNullOrWhiteSpace(data.PlateNumber))
                {
                    var carId = tr.SelectScalar<int>(Sql.GetCarId, new { driverId = data.Id });
                    if (carId > 0)
                    {//修改车辆信息
                        var uc = tr.Update(Sql.UpdateCar, new { PlateNumber = data.PlateNumber, carId = carId });
                        if (uc > 0 & u > 0)
                        {
                            tr.Commit();
                            return ApiResult.Create(true, "修改车辆、人员信息成功");
                        }
                        return ApiResult.Create(false, "修改车辆、人员信息失败");
                    }
                    if (carId <= 0 & data.PlateNumber != null)
                    {//绑定车辆员工信息
                        var ic = tr.Update(Sql.InsertEmployeeCar, new { CompanyId = Convert.ToInt32(Request.Properties["CompanyId"]), PlateNumber = data.PlateNumber, EmployeeId = data.Id });
                        if (ic > 0 & u > 0)
                        {
                            tr.Commit();
                            return ApiResult.Create(true, "绑定车辆、更新员工信息成功");
                        }
                        return ApiResult.Create(false, "绑定车辆、更新员工信息不成功");
                    }

                }
                if (u > 0)
                {
                    tr.Commit();
                    return ApiResult.Create(true, "操作成功");
                }
                return ApiResult.Create(false, "操作失败");
            }
        }



        private class DepartmentData
        {
            public int EmployeeId { get; set; }
            public int DepartmentId { get; set; }
        }
        public class AddAuthorityData
        {
            public List<int> EmployeeId { get; set; }
        }
        public class EmployeeList
        {
            public object ChildrenDept { get; set; }
            public object Employee { get; set; }
        }
        public class UpdateDepartmentData
        {
            public List<int> DepartmentId { get; set; }
            public List<int> EmployeeId { get; set; }
        }
        public class UpdateEmployeeData
        {
            public int Id { get; set; }
            public string Tel { get; set; }
            public string Name { get; set; }
            public string Password { get; set; }
            public string PlateNumber { get; set; }
        }
        public class AddEmployeeData
        {
            public int deptId { get; set; }
            public string Name { get; set; }
            public string Tel { get; set; }
            public string Password { get; set; }
            public string PlateNumber { get; set; }

            [JsonIgnore]
            public int CompanyId { get; set; }
        }
        public class AddDeptData
        {
            public string Name { get; set; }
            public int parentid { get; set; }
            [JsonIgnore]
            public int AuthorityStatus { get; set; }
            [JsonIgnore]
            public int CompanyId { get; set; }
        }
    }
}