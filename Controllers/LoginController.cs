using AppHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/Login")]
    public class LoginController : ApiController
    {
        /// <summary>
        /// 登陆类型
        /// </summary>
        public enum LoginType
        {
            PC登录, 钉钉
        }


        /// <summary>
        /// 登陆返回带权限的数据
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="type">登陆类型</param>
        /// <returns></returns>
        [HttpGet, Route("AuthLogin")]
        public object Login(string userName, string password, LoginType type)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                //throw new Exception("测试");
                var result = c.Select<DingLoginInfo>(Sql.GetAuthLoginSql, new { name = userName, password }).FirstOrDefault();
                if (result == null)
                {
                    return ApiResult.Create(false, "登录失败");
                }
                return ApiResult.Create(true, result, "登录成功");
            }
        }



        class AuthData
        {
            public string Auth { get; set; }
            public string Token { get; set; }
        }
        class DingLoginInfo
        {
            [JsonIgnore]
            public int Id { get; set; }

            [JsonIgnore]
            public string AuthList { get; set; }
            public List<AuthData> Auth
            {
                get
                {
                    var userTypeArray = AuthList.Split(';').ToList();


                    return userTypeArray.ConvertAll(d =>
                    new AuthData()
                    {
                        Auth = d,
                        Token = (Type + "\n" + Id + "\n" + d + "\n" + CompanyId + "\n" + DateTime.Now.ToString()).Encode()//分别为登陆类型，登陆Id，登陆用户权限，公司ID，时间戳
                    }
                    );
                }
            }

            [JsonIgnore]
            public int CompanyId { get; set; }
            [JsonIgnore]
            public LoginType Type { get; set; }



        }
        class LoginData
        {
            public bool Status { get; set; }
            public string Message { get; set; }
            public dynamic Result { get; set; }
        }

    }
}
