using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SampleApp.Middleware;
using Shouldly;
using StackExchange.Redis;

namespace AspNetCore.Middleware.Samples
{
    public class RedisCacheMiddlewareTests
    {
        private Mock<HttpContext> context;
        private Mock<IDatabase> database;
        private Mock<HttpRequest> request;
        private Mock<HttpResponse> response;

        [SetUp]
        public void Setup()
        {
            database = new Mock<IDatabase>();
            context = new Mock<HttpContext>();
            request = new Mock<HttpRequest>();
            response = new Mock<HttpResponse>();

            request.SetupProperty(r => r.Body);
            response.SetupProperty(r => r.Body);
            response.SetupProperty(r => r.StatusCode);
            context.Setup(c => c.Request)
                .Returns(() => request.Object);
            context.Setup(c => c.Response)
                .Returns(() => response.Object);
        }

        [Test]
        [TestCase("/somePath", "Hello, World!")]
        public async Task ShouldTakeFromCache(string path, string responseText)
        {
            // Given
            var middleware = new RedisCacheMiddleware(
                new OptionsWrapper<RedisCacheMiddlewareOptions>(
                    new RedisCacheMiddlewareOptions()), Task.FromResult(database.Object));
            var ms = new MemoryStream();

            database.Setup(db => db.StringGetAsync(path, It.IsAny<CommandFlags>()))
                .ReturnsAsync(responseText);
            request.Setup(r => r.Path).Returns(path);
            response.Object.Body = ms;
            response.Setup(r => r.BodyWriter).Returns(PipeWriter.Create(ms));

            // When
            await middleware.InvokeAsync(context.Object, async _ => { });

            // Then
            ms.ReadToEnd().ShouldBe(responseText);
        }

        [Test]
        [TestCase("/somePath", "Hello, World!")]
        public async Task ShouldWriteToCache(string path, string responseText)
        {
            // Given
            var middleware = new RedisCacheMiddleware(
                new OptionsWrapper<RedisCacheMiddlewareOptions>(
                    new RedisCacheMiddlewareOptions()), Task.FromResult(database.Object));
            var ms = new MemoryStream();

            request.Setup(r => r.Path).Returns(path);
            response.Object.Body = ms;
            response.Setup(r => r.BodyWriter).Returns(() => PipeWriter.Create(response.Object.Body));

            // When
            await middleware.InvokeAsync(context.Object, async c =>
            {
                c.Response.StatusCode = 200;
                await c.Response.WriteAsync(responseText);
            });

            // Then
            database.Verify(db =>
                db.StringSetAsync(path, It.IsAny<RedisValue>(), null, It.IsAny<When>(), It.IsAny<CommandFlags>()));
        }
    }

    public static class StreamTestExtensions
    {
        public static string ReadToEnd(this MemoryStream ms)
        {
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}