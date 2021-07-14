using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SampleApp.Middleware
{
    public class CookieVisitorTrackerMiddleware : IMiddleware
    {
        private readonly CookieVisitorTrackerMiddlewareOptions _options;

        public CookieVisitorTrackerMiddleware(IOptions<CookieVisitorTrackerMiddlewareOptions> options)
        {
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var feature = context.Request.Cookies.TryGetValue(_options.CookieKey, out var cookie)
                ? new VisitorIdFeature(cookie)
                : new VisitorIdFeature();

            if (feature.IsFirstTimeVisitor)
                context.Response.OnStarting(async () =>
                {
                    if (context.Response.StatusCode == (int) HttpStatusCode.OK)
                        context.Response.Cookies.Append(_options.CookieKey, feature.VisitorId, new CookieOptions
                        {
                            MaxAge = _options.MaxAge
                        });
                });

            context.Features.Set<IVisitorIdFeature>(feature);

            await next(context);
        }
    }
}