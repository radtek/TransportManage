using System;
using System.Collections.Generic;
using System.Linq;

namespace TransportManage.Utilities
{
    public class PagingMethod
    {
        /// <summary>
        /// 对数据进行分页操作
        /// </summary>
        /// <param name="db">原始数据</param>
        /// <param name="pageCount">一页多少行数据10</param>
        /// <param name="pageNumber">一共多少页</param>
        /// <returns></returns>

        internal static IEnumerable<Controllers.OrderController.TaskInfo> Paging(IList<Controllers.OrderController.TaskInfo> db, int pageCount, int pageNumber)
        {
            if (db == null)
            {
                throw new ArgumentNullException("进行分页的数据不能为空");
            }

            return db.OrderByDescending(d => d.CompleteTime).Skip(pageCount * pageNumber).Take(pageCount);
        }
    }
}