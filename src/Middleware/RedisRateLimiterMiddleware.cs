using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SampleApp.Awaitable;
using StackExchange.Redis;

namespace SampleApp.Middleware
{
    public class RedisRateLimiterMiddleware : IMiddleware
    {
        private readonly ISystemClock _systemClock;
        private readonly ITaskAwaitable<IDatabase> _db;
        private readonly RateLimiterMiddlewareOptions _options;

        public RedisRateLimiterMiddleware(
            ISystemClock systemClock,
            ITaskAwaitable<IDatabase> db,
            IOptions<RateLimiterMiddlewareOptions> options)
        {
            _systemClock = systemClock;
            _db = db;
            _options = options.Value;
        }


        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var db = await _db;
            var now = _systemClock.UtcNow.DateTime.ToLocalTime();
            var value = await db.ListGetByIndexAsync(_options.RedisKey, 0);
            var lastRequest = DateTime.FromFileTime((long)value);

            if (lastRequest >= now - _options.Interval)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] =
                    (lastRequest - now + _options.Interval).TotalSeconds.ToString("#");
            }
            else
            {
                await db.ListRightPushAsync(_options.RedisKey, now.ToFileTime());
                await db.ListTrimAsync(_options.RedisKey, -_options.MaxRequests, -1);
                await next(context);
            }
        }
    }
}