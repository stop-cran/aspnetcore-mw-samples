using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using SampleApp.Middleware;
using StackExchange.Redis;

namespace SampleApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services
                .AddSingleton<Func<DateTime>>(() => DateTime.Now)
                .AddTransient<CookieVisitorTrackerMiddleware>()
                .Configure<CookieVisitorTrackerMiddlewareOptions>(Configuration.GetSection("Visitor"))
                .AddSingleton<RateLimiterMiddleware>()
                .AddSingleton<RedisRateLimiterMiddleware>()
                .Configure<RateLimiterMiddlewareOptions>(Configuration.GetSection("RateLimiter"))
                .AddTransient<RedisCacheMiddleware>()
                .Configure<RedisCacheMiddlewareOptions>(Configuration.GetSection("RedisCache"));

            services
                .AddSingleton<Task<IConnectionMultiplexer>>(async _ =>
                    await ConnectionMultiplexer.ConnectAsync(Configuration["RedisCache:ConnectionString"]))
                .AddTransient(async container =>
                {
                    var connection = await container.GetRequiredService<Task<IConnectionMultiplexer>>();

                    return connection.GetDatabase();
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseHttpMetrics();
            app.UseCookieVisitorId();
            app.UseRedisCache("/test_cache");
            app.UseRateLimiterForNewVisitors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => await context.Response.WriteAsync("Hello World!"));
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });
        }
    }
}