using System.Net.Http;
using System.Web.Http;

namespace TransportManage.Models
{
    /// <summary>
    /// 渣土车系统登录信息
    /// </summary>
    public class UserInfoData
    {
        private HttpRequestMessage Request { get; set; }
        private string UserInfo { get { return Request.Properties["UserType"].ToString(); } }
        public bool IsBoss { get { return UserInfo.IndexOf("超管") >= 0; } }
        public bool IsAdmin { get { return UserInfo.IndexOf("工管") >= 0 && UserInfo.IndexOf("超管") == -1; } }
        public bool IsDriver { get { return (IsBoss || IsAdmin) == false; } }
        public UserInfoData(HttpRequestMessage Parameter)
        {
            this.Request = Parameter;
        }
    }
}