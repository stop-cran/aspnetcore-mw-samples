using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SampleApp.Middleware
{
    public static class RedisCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseRedisCache(this IApplicationBuilder app, PathString path) =>
            app.UseWhen(c => c.Request.Method == HttpMethods.Get && c.Request.Path == path,
                branch => branch.UseMiddleware<RedisCacheMiddleware>());

        public static IApplicationBuilder UseCookieVisitorId(this IApplicationBuilder app)
        {
            app.ServerFeatures.Set<IVisitorIdFeature>(new VisitorIdFeature(string.Empty));

            return app.UseMiddleware<CookieVisitorTrackerMiddleware>();
        }

        public static IApplicationBuilder UseRateLimiterForNewVisitors(this IApplicationBuilder app)
        {
            return app.UseWhen(context => context.Features.Get<IVisitorIdFeature>()?.IsFirstTimeVisitor ?? false,
                app => app.UseMiddleware<RateLimiterMiddleware>());
        }
    }
}