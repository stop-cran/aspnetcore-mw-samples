using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SampleApp.Awaitable;
using StackExchange.Redis;

namespace SampleApp.Middleware
{
    public class RedisCacheMiddleware : IMiddleware
    {
        private readonly IOptions<RedisCacheMiddlewareOptions> _options;
        private readonly ITaskAwaitable<IDatabase> _redisTask;

        public RedisCacheMiddleware(
            IOptions<RedisCacheMiddlewareOptions> options,
            ITaskAwaitable<IDatabase> redisTask)
        {
            _options = options;
            _redisTask = redisTask;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var db = await _redisTask;

            var cachedResponse = await db.StringGetAsync(context.Request.Path.ToString());

            if (cachedResponse.HasValue)
            {
                await context.Response.WriteAsync(cachedResponse);
            }
            else
            {
                var responseStream = context.Response.Body;
                await using var ms = new MemoryStream();

                context.Response.Body = ms;
                await next.Invoke(context);

                if (context.Response.StatusCode == (int) HttpStatusCode.OK)
                    await db.StringSetAsync(context.Request.Path.ToString(), ms.ToArray(),
                        _options.Value.Ttl);
                context.Response.Body = responseStream;
                await context.Response.Body.WriteAsync(ms.ToArray());
            }
        }
    }
}