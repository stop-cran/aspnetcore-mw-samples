using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SampleApp.Awaitable;
using SampleApp.Middleware;
using Shouldly;
using StackExchange.Redis;
using IServer = Microsoft.AspNetCore.Hosting.Server.IServer;

namespace AspNetCore.Middleware.Samples
{
    public class IntegrationTests
    {
        private CancellationTokenSource _cancel;
        private IHost host;
        private Mock<Func<string>> messageFactory;

        [SetUp]
        public void Setup()
        {
            messageFactory = new Mock<Func<string>>();
            _cancel = new CancellationTokenSource(1000);

            host = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webHostBuilder =>
                    webHostBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                            services
                                .AddSingletonTaskAwaitable<IConnectionMultiplexer>(async _ =>
                                    await ConnectionMultiplexer.ConnectAsync("localhost"))
                                .AddTransient<RedisCacheMiddleware>()
                                .AddSingleton<IOptions<RedisCacheMiddlewareOptions>>(
                                    new OptionsWrapper<RedisCacheMiddlewareOptions>(
                                        new RedisCacheMiddlewareOptions
                                        {
                                            Ttl = TimeSpan.FromSeconds(2)
                                        }))
                                .AddTransientTaskAwaitable(async container =>
                                {
                                    var connection = await container.GetRequiredService<ITaskAwaitable<IConnectionMultiplexer>>();

                                    return connection.GetDatabase();
                                })
                                .AddControllers())
                        .Configure(app => app
                            .UseDeveloperExceptionPage()
                            .UseRedisCache("/test")
                            .UseRouting()
                            .UseEndpoints(endpoints =>
                                endpoints.MapGet("/test",
                                    async context => { await context.Response.WriteAsync(messageFactory.Object()); }))
                        ))
                .Build();
        }

        [TearDown]
        public void TearDown()
        {
            host.Dispose();
            _cancel.Dispose();
        }

        [Test]
        [TestCase("test message")]
        public async Task ShouldCache(string message)
        {
            // Given
            messageFactory.Setup(f => f()).Returns(message);

            // When
            await host.StartAsync();

            using var client = host.Services
                .GetRequiredService<IServer>()
                .ShouldBeOfType<TestServer>()
                .CreateClient();

            var response1 = await client.GetAsync("/test");
            var response2 = await client.GetAsync("/test");
            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            //Then
            content1.ShouldBe(message);
            content2.ShouldBe(message);
            messageFactory.Verify(f => f(), Times.Once());
        }
    }
}