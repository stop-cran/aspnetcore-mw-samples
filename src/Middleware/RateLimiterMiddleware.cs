using System;
using System.Net;
using System.Threading.Tasks;
using Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SampleApp.Middleware
{
    public class RateLimiterMiddleware : IMiddleware
    {
        private readonly Func<DateTime> _nowFactory;
        private readonly RateLimiterMiddlewareOptions _options;
        private readonly CircularBuffer<DateTime> newVisitorRequests;

        public RateLimiterMiddleware(
            Func<DateTime> nowFactory,
            IOptions<RateLimiterMiddlewareOptions> options)
        {
            _nowFactory = nowFactory;
            _options = options.Value;
            newVisitorRequests = new CircularBuffer<DateTime>(_options.MaxRequests);
            newVisitorRequests.Put(_nowFactory() - _options.Interval);
        }


        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var now = _nowFactory();
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