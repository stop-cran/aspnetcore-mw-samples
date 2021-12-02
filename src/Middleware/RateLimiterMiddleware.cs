using System;
using System.Net;
using System.Threading.Tasks;
using Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SampleApp.Middleware
{
    public class RateLimiterMiddleware : IMiddleware
    {
        private readonly ISystemClock _systemClock;
        private readonly RateLimiterMiddlewareOptions _options;
        private readonly CircularBuffer<DateTime> newVisitorRequests;

        public RateLimiterMiddleware(
            ISystemClock systemClock,
            IOptions<RateLimiterMiddlewareOptions> options)
        {
            _systemClock = systemClock;
            _options = options.Value;
            newVisitorRequests = new CircularBuffer<DateTime>(_options.MaxRequests);
            newVisitorRequests.Put(_systemClock.UtcNow.DateTime.ToLocalTime().AddMilliseconds(-1) - _options.Interval);
        }


        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var now = _systemClock.UtcNow.DateTime.ToLocalTime();
            if (context.Features.Get<IVisitorIdFeature>()?.IsFirstTimeVisitor ?? false)
            {
                var lastRequest = newVisitorRequests.Peek();

                if (lastRequest >= now - _options.Interval)
                {
                    context.Response.StatusCode = (int) HttpStatusCode.TooManyRequests;
                    context.Response.Headers["Retry-After"] =
                        (lastRequest - now + _options.Interval).TotalSeconds.ToString("#");
                }
                else
                {
                    newVisitorRequests.Put(now);
                    await next(context);
                }
            }
            else
            {
                await next(context);
            }
        }
    }
}