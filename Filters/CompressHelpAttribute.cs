using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Filters;

namespace TransportManage.Filters
{
    public class CompressHelpAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var content = actionExecutedContext.Response.Content;
            var acceptEncoding = actionExecutedContext.Request.Headers.AcceptEncoding.Where(x => x.Value == "gzip" || x.Value == "deflate").ToList();
            if (acceptEncoding != null && acceptEncoding.Count > 0 && content != null && actionExecutedContext.Request.Method != HttpMethod.Options)
            {
                var bytes = content.ReadAsByteArrayAsync().Result;
                if (acceptEncoding.FirstOrDefault().Value == "deflate")
                {
                    actionExecutedContext.Response.Content = new ByteArrayContent(CompressionHelper.GzipCompress(bytes));
                    actionExecutedContext.Response.Content.Headers.Add("Content-Encoding", "deflate");
                    //actionExecutedContext.Response.Content.Headers.Add("Content-Type", "Json/Application");
                }
                else if (acceptEncoding.FirstOrDefault().Value == "gzip")
                {
                    actionExecutedContext.Response.Content = new ByteArrayContent(CompressionHelper.DeflateCompress(bytes));
                    actionExecutedContext.Response.Content.Headers.Add("Content-Type", "application/json");
                    // actionExecutedContext.Response.Content.Headers.Add("Content-encoding", "gzip");
                }
            }
            base.OnActionExecuted(actionExecutedContext);
        }

    }
    class CompressionHelper
    {

        public static byte[] DeflateCompress(byte[] data)
        {
            if (data == null || data.Length < 1)
                return data;
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (DeflateStream gZipStream = new DeflateStream(stream, CompressionMode.Compress))
                    {
                        gZipStream.Write(data, 0, data.Length);
                        gZipStream.Close();
                    }
                    return stream.ToArray();
                }
            }
            catch (Exception)
            {
                return data;
            }
        }

        public static byte[] GzipCompress(byte[] data)
        {
            if (data == null || data.Length < 1)
                return data;
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Compress))
                    {
                        gZipStream.Write(data, 0, data.Length);
                        gZipStream.Close();
                    }
                    return stream.ToArray();
                }
            }
            catch (Exception)
            {
                return data;
            }

        }
    }

}