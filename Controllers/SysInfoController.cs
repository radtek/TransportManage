using Dapper;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [MyExceptionFilter]
    [TokenFilter(3, new string[] { "Type", "Id", "Time" }, 2, '\n', "Login")]
    public class SysInfoController : ApiController
    {
        /// <summary>
        /// 获取登陆公司客户或司机车辆信息
        /// </summary>
        /// <param name="token"></param>
        /// <param name="type">0为客户的信息，1为司机的信息</param>
        /// <returns></returns>
        [HttpGet, Route("api/SysInfo/GetInfo")]
        public dynamic GetInfo(string token, InfoType type)
        {
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                var Id = Request.Properties["Id"];
                string sql = null;
                switch (type)
                {
                    case InfoType.Client:
                        sql = Sql.GetClientInfo;
                        break;
                    case InfoType.Driver:
                        sql = Sql.GetDriverInfo;
                        break;
                    default:
                        break;
                }
                return new ApiResult<dynamic>() { Status = true, Result = c.Query(sql, new { Id }) };
            }
        }

    }
    public enum InfoType
    {
        /// <summary>
        /// 老板
        /// </summary>
        Client,
        /// <summary>
        /// 司机
        /// </summary>
        Driver
    }
}
