using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace SampleApp.Middleware
{
    public class RedisRateLimiterMiddleware : IMiddleware
    {
        private readonly Func<DateTime> _nowFactory;
        private readonly Task<IDatabase> _db;
        private readonly RateLimiterMiddlewareOptions _options;

        public RedisRateLimiterMiddleware(
            Func<DateTime> nowFactory,
            Task<IDatabase> db,
            IOptions<RateLimiterMiddlewareOptions> options)
        {
            _nowFactory = nowFactory;
            _db = db;
            _options = options.Value;
        }


        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var db = await _db;
            var now = _nowFactory();
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