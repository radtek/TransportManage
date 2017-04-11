using System.Data.SqlClient;

namespace TransportManage.Models
{
    public static class AppDb
    {
        internal const string TransportManager = "渣土车";
        internal const string GIAmapGetAddress2 = "位置信息";

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <returns></returns>
        public static SqlConnection CreateConnection(string typeContect)
        {
            var a = InitConnectionString(typeContect);
            return new SqlConnection(a);
        }
        private static string InitConnectionString(string dbconnect)
        {
            var c = System.Web.Configuration.WebConfigurationManager.ConnectionStrings[dbconnect];
            if (c == null)
            {
                throw new System.Configuration.ConfigurationErrorsException("Web.config 缺少" + dbconnect + "连接字符串配置。");
            }
            return c.ConnectionString;
        }
    }
    //class ConnectionPool : System.IDisposable
    //{
    //    // IntPtr _Handle;
    //    List<SqlConnection> _connections;
    //    public ConnectionPool()
    //    {

    //    }
    //    #region IDisposable Support
    //    private bool disposedValue = false; // 要检测冗余调用

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (!disposedValue)
    //        {
    //            if (disposing)
    //            {
    //                // TODO: 释放托管状态(托管对象)。
    //                foreach (var item in _connections)
    //                {
    //                    try
    //                    {
    //                        item.Dispose();
    //                    }
    //                    catch (System.Exception)
    //                    {
    //                        // go on
    //                    }
    //                }
    //            }

    //            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
    //            // TODO: 将大型字段设置为 null。
    //            //NativeMethods.ReleaseHandle(_Handle);
    //            //_Handle = System.IntPtr.Zero;
    //            disposedValue = true;
    //        }
    //    }

    //    // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
    //    //~ConnectionPool()
    //    //{
    //    //    // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //    //    Dispose(false);
    //    //}

    //    // 添加此代码以正确实现可处置模式。
    //    public void Dispose()
    //    {
    //        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //        Dispose(true);
    //        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
    //        //System.GC.SuppressFinalize(this);
    //    }
    //    #endregion

    //}
}