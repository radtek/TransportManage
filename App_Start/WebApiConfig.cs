using System.Web.Http;

namespace TransportManage
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务
            //config.Routes.IgnoreRoute("elmah", "swagger/ui/{index.html}");
            // Web API 路由


            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            //config.MessageHandlers.Insert()
            //config.Routes.MapHttpRoute(
            //    name: "GetCompleteTaskApi",
            //    routeTemplate: "api/Admin/GetCompleteTask/{startTime}/{endTime}",
            //    defaults: new { startTime = "2000/1/1", endTime = System.DateTime.Now.Date }
            //    );
            //config.MessageHandlers.Add(new CompressMessageHandler());
            //var r = config.Routes.VirtualPathRoot;
        }
    }
}
