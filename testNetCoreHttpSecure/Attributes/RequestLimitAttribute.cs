using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace testNetCoreHttpSecure.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequestLimitAttribute : ActionFilterAttribute
    {
        private readonly string _name;
        private readonly int _noOfRequest;
        private readonly int _seconds;

        public RequestLimitAttribute(string name, int noOfRequest = 5, int seconds = 10)
        {
            _name = name;
            _noOfRequest = noOfRequest;
            _seconds = seconds;
        }

        // public string Name { get; }
        //
        // public int NoOfRequest { get; set; }
        //
        // public int Seconds { get; set; }

        // private static MemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

            var ipAddress = context.HttpContext.Request.HttpContext.Connection.RemoteIpAddress;
            var memoryCacheKey = $"{_name}-{ipAddress}";

            cache.TryGetValue(memoryCacheKey, out int prevReqCount);

            if (prevReqCount >= _noOfRequest)
            {
                context.Result = new ContentResult
                {
                    Content = $"Request limit is exceeded. Try again in {_seconds} seconds.",
                };

                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            }
            else
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_seconds));
                cache.Set(memoryCacheKey, prevReqCount + 1, cacheEntryOptions);
            }
        }
    }
}