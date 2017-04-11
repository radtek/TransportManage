using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using TransportManage.Filters;
using TransportManage.Models;

namespace TransportManage.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("Api/Statistics")]
    [TokenFilter(5, new string[] { "Type", "Id", "UserType", "CompanyId", "Time" }, 2, '\n', "Login")]
    public class MaintainController : ApiController
    {
        /// <summary>
        /// 司机保养，维修车辆
        /// </summary>
        /// <param name="pic">图片数组</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost, Route("AddCarMaintain")]
        public object AddCarMaintain(List<String> pic, string token)
        {
            var _userInfo = new UserInfoData(Request);
            using (var c = AppDb.CreateConnection(AppDb.TransportManager))
            {
                return null;
            }
        }
    }
}
