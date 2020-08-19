using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace testNetCoreHttpSecure.Middlewares
{
    public class DosAttackMiddleware
    {
        private const int BANNED_REQUESTS = 10;
        private const int REDUCTION_INTERVAL = 10;
        private const int RELEASE_INTERVAL = 1 * 60 * 1000; // 1 minutes    

        private static readonly Stack<string> _Banned = new Stack<string>();

        private readonly RequestDelegate _next;

        public DosAttackMiddleware(RequestDelegate next)
        {
            CreateBanningTimer();

            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();

            var ip = httpContext.Connection.RemoteIpAddress.ToString();

            if (_Banned.Contains(ip))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                return;
            }

            CheckIpAddress(cache, ip);

            await _next(httpContext);
        }

        private static void CheckIpAddress(IMemoryCache memoryCache, string ip)
        {
            var key = $"Dos-{ip}";

            if (memoryCache.TryGetValue(key, out CacheObject cacheObj))
            {
                cacheObj.Count++;

                if (cacheObj.Count == BANNED_REQUESTS)
                {
                    memoryCache.Remove(key);
                    _Banned.Push(ip);
                }
                else
                {
                    memoryCache.Set(key, cacheObj, cacheObj.ExpirationOffset);
                }
            }
            else
            {
                var dateTimeOffset = DateTimeOffset.Now.AddSeconds(REDUCTION_INTERVAL);

                var cacheObject = new CacheObject
                {
                    Count = 1,
                    ExpirationOffset = dateTimeOffset
                };

                memoryCache.Set(key, cacheObject, dateTimeOffset);
            }
        }

        private static void CreateBanningTimer()
        {
            Console.WriteLine($"{nameof(CreateBanningTimer)}");

            var timer = GetTimer(RELEASE_INTERVAL);

            timer.Elapsed += delegate
            {
                if (_Banned.Any())
                {
                    var pop = _Banned.Pop();

                    Console.WriteLine($"banned pop - {pop}, {DateTime.Now:O}");
                }
            };
        }

        private static Timer GetTimer(int interval)
        {
            var timer = new Timer
            {
                Interval = interval
            };

            timer.Start();

            return timer;
        }

        private class CacheObject
        {
            public int Count { get; set; }

            public DateTimeOffset ExpirationOffset { get; set; }
        }
    }
}