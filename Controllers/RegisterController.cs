using Dapper;
using System;
using System.Linq;
using System.Web.Http;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    public class RegisterController : ApiController
    {
        [HttpGet, Route("Api/Register/DriverCar")]
        public dynamic RegisterDriver()
        {
            return null;
        }

        /// <summary>
        /// 客户、司机注册获取微信公众号唯一凭证码
        /// </summary>
        /// <param name="code">微信Code</param>
        /// <param name="authCode">系统验证码</param>
        /// <returns></returns>
        public dynamic Get(string code, string authCode)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new ApiResult<string>()
                {
                    Status = false,
                    Result = "不能获取微信Code"
                };
            }
            if (string.IsNullOrWhiteSpace(authCode))
            {
                return new ApiResult<string>()
                {
                    Status = false,
                    Result = "验证码不能为空"
                };
            }



            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var t = c.Query<SelectData>(Sql.Code, new { code = authCode }).FirstOrDefault();
                if (t == null)
                {
                    return new ApiResult<string>()
                    {
                        Status = false,
                        Result = "请验证验证码是否正确"
                    };
                }
                //获取OpenId
                string openId = Push.getAccessToken.GetOpenid(code, "wx28e22283e44effe1", "e2bacb6a800916e6f276b70150affdb2").oauth2_accessToken["openid"].ToString();
                if (t.Expiration > DateTime.Now)
                {
                    return new ApiResult<string>
                    {
                        Status = false,
                        Result = "验证过期"
                    };
                }
                if (t.ClientId != 0)
                {
                    c.Execute(Sql.RegisterClient, new { t.ClientId, openId });
                }
                if (t.DriverId != 0)
                {
                    c.Execute(Sql.RegisterDriver, new { t.DriverId, openId });
                }
                return new ApiResult<string>()
                {
                    Status = true,
                    Result = "验证通过"
                };

            }
        }
    }
    class SelectData
    {
        public int ClientId { get; set; }
        public int DriverId { get; set; }
        public DateTime Expiration { get; set; }

    }
}
