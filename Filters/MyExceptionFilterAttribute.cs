using AppHelpers;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace TransportManage.Filters
{
    public class MyExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            actionExecutedContext.Exception.Log();
            actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.OK);
            actionExecutedContext.Response.Content =
                new StringContent(JsonConvert.SerializeObject(Models.ApiResult.Create(false, actionExecutedContext.Exception.Message.ToString(), HttpStatusCode.InternalServerError)));
        }
    }
}