using AppHelpers;
using Dapper;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/EnterInfo")]
    public class EnterInfoController : ApiController
    {
        /// <summary>
        /// 录入司机车辆信息
        /// </summary>
        /// <param name="data">具体数据(需手动填充默认插入部门，以及公司ID)</param>
        /// <returns></returns>
        [HttpPost, Route("DriverCarInfo")]
        public dynamic DriverCarInfo(DriverCar data)
        {
            if (data.Password == null)
            {
                return ApiResult.Create(false, "Password不能为空");
            }
            if (data.Name == null)
            {
                return ApiResult.Create(false, "Name不能为空");

            }
            if (data.Tel == null)
            {
                return ApiResult.Create(false, "Tel不能为空");

            }
            if (data.PlateNumber == null)
            {
                return ApiResult.Create(false, "PlateNumber不能为空");

            }
            if (data.CompanyId == 0)
            {
                return ApiResult.Create(false, "公司Id不能为空");
            }
            if (data.DepartmentId == 0)
            {
                return ApiResult.Create(false, "部门Id不能为空");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            using (var tr = c.OpenTransaction())
            {

                //插入Employee,并且记录记录时间
                //注意 conpanyId 部署时需参数化
                var driverId = tr.SelectScalar<int>(Sql.InsertDriverInfo, new { data.Name, data.Tel, data.Password, CompanyId = data.CompanyId });
                var carId = tr.SelectScalar<int>(Sql.InsertCarInfo, new
                {
                    PlateNumber = data.PlateNumber,
                    CompanyId = data.CompanyId
                });

                var r = tr.Update(Sql.InsertDriverCar, new
                {
                    EmployeeId = driverId,
                    CarId = carId
                });
                if (r == 0)
                {
                    return ApiResult.Create(false, "录入信息失败");
                }

                //注意 默认插入部门ID 部署时需参数化
                var insertDepartmentResult = tr.Update(Sql.InsertDepartment, new
                {
                    EmployeeId = driverId,
                    DepartmentId = data.DepartmentId

                });
                var insertEmployeeLevelResult = tr.Update(Sql.InsertEmployeeLevel, new
                {
                    Name = "司机",
                    EmployeeId = driverId
                });

                if (insertDepartmentResult > 0 & insertEmployeeLevelResult > 0)
                {
                    tr.Commit();
                    return ApiResult.Create(true, "录入信息成功");
                }
                else
                {
                    return ApiResult.Create(false, "录入信息失败");
                }
            }

        }


        /// <summary>
        /// 录入报备证据
        /// </summary>
        /// <returns></returns>
        [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
        [HttpPost, Route("ReportedEvidence")]
        public dynamic ReportedEvidence(EvidenceData data, string token)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            using (var tr = c.OpenTransaction())
            {
                if (data.OriginalUrl.Count != 0)
                {
                    #region 获取图片名称URL
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
                    foreach (var item in requestList)
                    {
                        ActionHelper.RetryIfFailed<WebException>(() =>
                        {

                            using (HttpWebResponse response = (HttpWebResponse)item.GetResponse())
                            using (Stream r = response.GetResponseStream())
                            using (Image img = Image.FromStream(r))
                            {
                                string newFileName = DateTime.Now.ToString("yyyyMMddHHmmss_ffff", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".jpeg";
                                img.Save(Path.Combine(HttpContext.Current.Server.MapPath("~/photos/"), newFileName));
                                data.ImageUrl.Add(newFileName);
                            }
                        }, 5, 1000)
                    .Invoke();
                    }
                    #endregion
                }

                var userId = Convert.ToInt32(Request.Properties["Id"]);
                var companyId = Convert.ToInt32(Request.Properties["CompanyId"]);
                int dialogueId = c.ExecuteScalar<int>(Sql.InsertDialogue, new
                {
                    TaskId = data.TaskId,
                    Remark = data.Remark,
                    UserId = userId,
                    UserType = 3,//司机手机端，1PC超管？2工管手机端？
                    CompanyId = companyId
                }, tr);
                if (data.ImageUrl != null && data.ImageUrl.Count != 0)
                {//insert evidence photo and return table id
                    var d = data.ImageUrl.ConvertAll(a =>
                            new
                            {
                                TaskId = data.TaskId,
                                Url = a,
                                DialogueId = dialogueId
                            });
                    var er = tr.Update(Sql.InsertEvidence + Sql.UpdateDeliveryTask, new { TaskId = data.TaskId, DialogueId = dialogueId, }, data.ImageUrl.ConvertAll(dd => new { Url = dd }));
                    if (er == 0)
                    {
                        return ApiResult.Create(false, "操作失败");
                    }
                    tr.Commit();
                    return ApiResult.Create(true, "操作成功");
                }
                else
                {
                    return ApiResult.Create(false, "未能解析图片，请重新上传");
                }
            }
        }
    }
}

public class EvidenceData
{

    public string Remark { get; set; }
    public int TaskId { get; set; }
    [JsonIgnore]
    public List<string> ImageUrl { get; set; }
    public List<string> OriginalUrl { get; set; }
}

public class DriverCar
{
    public int CompanyId { get; set; }
    public string Name { get; set; }
    public string PlateNumber { get; set; }
    public string Tel { get; set; }
    public string Password { get; set; }
    public int DepartmentId { get; set; }
}

