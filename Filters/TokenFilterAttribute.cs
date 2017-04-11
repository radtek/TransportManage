using AppHelpers;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace TransportManage.Filters
{
    public sealed class TokenFilterAttribute : ActionFilterAttribute
    {
        private int _tokenArraryCount { get; set; }
        private string[] _tokenName { get; set; }
        private string _ingoreControllerName { get; set; }
        private int _time { get; set; }
        private char _spiltchar { get; set; }
        /// <summary>
        /// token构造函数
        /// </summary>
        /// <param name="tokenArraryCount">token数组长度</param>
        /// <param name="time">token失效时间单位为1小时</param>
        /// <param name="tokenName">token各个节点名字，最后一个默认为时间节点</param>
        /// <param name="ingoreControllerName">忽略控制器的名字</param>
        public TokenFilterAttribute(int tokenArraryCount, string[] tokenName, int time, char spiltchar, string ingoreControllerName = "Login")
        {
            _tokenArraryCount = tokenArraryCount;
            _tokenName = tokenName;
            _time = time;
            _ingoreControllerName = ingoreControllerName;
            _spiltchar = spiltchar;

        }

        public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var q = actionContext.Request.RequestUri.ParseQueryString();
            var v = q.GetValues("token");
            if (v == null || v.Length == 0)
            {
                if (actionContext.ControllerContext.ControllerDescriptor.ControllerName.Contains("Login"))
                {
                    base.OnActionExecuting(actionContext);
                    return;
                }

                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "缺少 Token");
                return;
            }
            var t = v[0].Replace(' ', '+');
            var tp = t.Decode().Split('\n');
            if (tp.Length != _tokenArraryCount)
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Token 无效");
                return;
            }

            for (int i = 0; i < _tokenName.Length; i++)
            {
                actionContext.Request.Properties[_tokenName[i]] = tp[i];
            }
            //if ((DateTime.Now - tp[_tokenName.Length - 1].ToDateTime(DateTime.MinValue)).TotalHours > _time)
            //{
            //    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Token 失效，请重新登录");
            //    return;
            //}
            base.OnActionExecuting(actionContext);
        }
    }

}