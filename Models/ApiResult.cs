using System.Net;

namespace TransportManage.Models
{
    public class ApiResult<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public int StatusNumber { get; set; }
        public T Result { get; set; }

    }

    public static class ApiResult
    {

        public static ApiResult<object> Create(bool status, string message)
        {
            return new ApiResult<object> { Status = status, Result = null, Message = message };
        }
        public static ApiResult<TResult> Create<TResult>(bool status, TResult result)
        {
            return new ApiResult<TResult> { Status = status, Result = result };
        }

        public static ApiResult<TResult> Create<TResult>(bool status, TResult result, string message)
        {
            return new ApiResult<TResult> { Status = status, Result = result, Message = message };
        }
        public static ApiResult<object> Create(bool status, string message, HttpStatusCode statusNumber)
        {
            return new ApiResult<object> { Status = status, Message = message, StatusNumber = (int)statusNumber };
        }
    }
}