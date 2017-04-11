using AppHelpers;
using Dapper;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/Order")]
    public class TestController : ApiController
    {
        /// <summary>
        /// 接收图片
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost, Route("ReceivePhoto")]
        public object ReceivePhoto(PhotoData data)
        {

            if (data.OriginalUrl.Count == 0)
            {
                return ApiResult.Create(false, "图片不能为空");
            }
            if (data.Lat == 0.0)
            {
                return ApiResult.Create(false, "未能获取经度");
            }
            if (data.Lng == 0.0)
            {
                return ApiResult.Create(false, "未能获取维度");
            }
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {

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
                            var astringSize = g.MeasureString(data.Address, f);
                            var tstringSize = g.MeasureString(sysTime, f);
                            var ap = new PointF(img.Width - astringSize.Width, img.Height - astringSize.Height);
                            var tp = new PointF(img.Width - tstringSize.Width, img.Height - astringSize.Height - tstringSize.Height - 2);
                            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            g.DrawString(data.Address, f, brush, ap);
                            g.DrawString(sysTime, f, brush, tp);
                            string newFileName = DateTime.Now.ToString("yyyyMMddHHmmss_ffff", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".jpeg";
                            img.Save(Path.Combine(HttpContext.Current.Server.MapPath("~/SitePhotos/"), newFileName));
                            data.ImageUrl.Add(newFileName);
                        }
                    }, 5, 1000)
                .Invoke();
                }


                var pId = c.ExecuteScalar<int>(Sql.InsertPhotos, new { data.Lng, data.Lat, data.Name, data.Type, data.Address }, tr);
                var result = tr.Update(Sql.InsertPhotosDetail, new { SitePicturesId = pId }, data.ImageUrl.ConvertAll(d => new { Url = d }));
                if (result > 0 & pId > 0)
                {
                    tr.Commit();
                    return ApiResult.Create(true, "操作成功");
                }
                return ApiResult.Create(false, "操作失败");
            }
        }


        /// <summary>
        /// 获取所有的图片
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("GetPhoto")]
        public object GetPhoto()
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                using (var cmd = c.SetupCommand(Sql.GetPhoto))
                using (var imgReader = cmd.ExecuteReader())
                {
                    var photo = imgReader.ToEntities<PhotoData>();
                    imgReader.NextResult();
                    imgReader.JoinEntities(photo, d => d.UrlDetail, d => d.Id, "Id");
                    return ApiResult.Create(true, photo);
                }
            }

        }
        public class PhotoData
        {
            [JsonIgnore]
            public int Id { get; set; }
            public float Lng { get; set; }
            public float Lat { get; set; }
            public string Address { get; set; }
            [JsonIgnore]
            public List<string> ImageUrl { get; set; } = new List<string>();

            public List<UrlData> UrlDetail { get; set; } = new List<UrlData>();
            public string Type { get; set; }
            [ConstrainLength(50, Action = ConstraintAction.ThrowException, Trim = true)]

            public List<string> OriginalUrl { get; set; }
            public string Name { get; set; }

        }
        public class UrlData
        {
            public string Url { get; set; }
        }
    }
}
