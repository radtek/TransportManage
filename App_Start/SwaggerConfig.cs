using Swashbuckle.Application;
using System.Web.Http;
using TransportManage;
using TransportManage.Models;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace TransportManage
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v2", AppDb.TransportManager);
                        c.IncludeXmlComments(GetXmlCommentsPath());
                        c.DescribeAllEnumsAsStrings();

                    })
                .EnableSwaggerUi(c =>
                    {

                    });
        }

        private static string GetXmlCommentsPath()
        {
            //填写存放项目xml的目录
            return string.Format("{0}/bin/TransportManage.XML", System.AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
