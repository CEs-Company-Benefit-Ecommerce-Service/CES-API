using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Utilities
{
    public class ErrorResponse : Exception
    {
        public ErrorDetailResponse Error { get; private set; }

        public ErrorResponse(int statusCode, int errorCode, string message)
        {
            this.Error = new ErrorDetailResponse
            {
                StatusCode = statusCode,
                ErrorCode = errorCode,
                Message = message
            };
        }

        public ErrorResponse() { }

    }

    public class ErrorDetailResponse
    {
        public int StatusCode { get; set; }
        public int ErrorCode { get; set; }
        public string Message { get; set; }
    }
}
