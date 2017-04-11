using AppHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace TransportManage.Utilities
{
    internal static class CreateMessage
    {
        /// <summary>
        /// 创建返回信息
        /// </summary>
        /// <param name="jsonBuilder"></param>
        /// <returns></returns>
        internal static StringBuilder CreateJsonMessage(Action<StringBuilder> jsonBuilder)
        {
            var sb = new StringBuilder();
            sb.Append(@"{""Status"":true,""Result"":");
            jsonBuilder(sb);
            sb.Append("}");
            return sb;
        }
        internal static HttpResponseMessage CreateJsonMessage(object[] stringResource)
        {
            var dict = new Dictionary<object, object>();
            for (int i = 0; i < stringResource.Length; i += 2)
            {
                dict.Add(stringResource[i], stringResource[i + 1]);
            }
            var str = dict.Select(d =>
        string.Format("\"{0}\": {1}", d.Key, d.Value));
            return ("{" + string.Join(",", str) + "}").CreateJsonMessage();
        }

        internal static HttpResponseMessage CreateJsonMessage(this string jsonString)
        {

            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
            message.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            return message;
        }
        internal static HttpResponseMessage CreateJsonMessage<T>(this T entity)
        {
            return JsonConvert.SerializeObject(entity).CreateJsonMessage();

        }
        internal static string JointString(this string[] stringResource)
        {
            string str = "{";
            for (int i = 0; i < stringResource.Length; i++)
            {
                if (stringResource[i] == ":")
                {
                    str += stringResource[i];
                }
                else if (stringResource[i] == ",")
                {
                    str += stringResource[i];
                }
                else
                {
                    str += "\"" + stringResource[i] + "\"";
                }

            }
            str += "}";
            return str;
        }
    }
}