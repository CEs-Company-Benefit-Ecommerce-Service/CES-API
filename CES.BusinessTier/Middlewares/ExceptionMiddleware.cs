using CES.BusinessTier.Utilities;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Middlewares
{
    //public class ExceptionMiddleware : IMiddleware
    //{
    //    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    //    {
    //        try
    //        {
    //            await next(context);
    //        }
    //        catch (Exception exception)
    //        {
    //            var errorResult = new ErrorDetailResponse();
    //            errorResult.Message = exception.Message;

    //            if (exception is not ErrorResponse && exception.InnerException != null)
    //            {
    //                while (exception.InnerException != null)
    //                {
    //                    exception = exception.InnerException;
    //                }
    //            }

    //            switch (exception)
    //            {
    //                case ErrorResponse e:
    //                    errorResult.StatusCode = (int)e.Error.StatusCode;
    //                    errorResult.ErrorCode = (int)e.Error.ErrorCode;
    //                    if (e.Error.Message is not null)
    //                    {
    //                        errorResult.Message = e.Error.Message;
    //                    }

    //                    break;

    //                case KeyNotFoundException:
    //                    errorResult.StatusCode = (int)HttpStatusCode.NotFound;
    //                    break;

    //                default:
    //                    errorResult.StatusCode = (int)HttpStatusCode.InternalServerError;
    //                    break;
    //            }
    //            var response = context.Response;
    //            if (!response.HasStarted)
    //            {
    //                response.ContentType = "application/json";
    //                response.StatusCode = errorResult.StatusCode;
    //                await response.WriteAsync(JsonConvert.SerializeObject(errorResult));
    //            }
    //        }
    //    }
    //}
    public class ExceptionMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var errorResult = CreateErrorResult(exception);

            var response = context.Response;
            if (!response.HasStarted)
            {
                response.ContentType = "application/json";
                response.StatusCode = errorResult.StatusCode;
                await response.WriteAsync(JsonConvert.SerializeObject(errorResult));
            }
        }

        private ErrorDetailResponse CreateErrorResult(Exception exception)
        {
            var errorResult = new ErrorDetailResponse();
            errorResult.Message = exception.Message;

            if (exception is ErrorResponse errorException)
            {
                errorResult.StatusCode = (int)errorException.Error.StatusCode;
                errorResult.ErrorCode = (int)errorException.Error.ErrorCode;
                if (errorException.Error.Message is not null)
                {
                    errorResult.Message = errorException.Error.Message;
                }
            }
            else if (exception is KeyNotFoundException)
            {
                errorResult.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                errorResult.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            return errorResult;
        }
    }
}
