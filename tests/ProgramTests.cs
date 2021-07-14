using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using SampleApp;
using Shouldly;

namespace AspNetCore.Middleware.Samples
{
    public class ProgramTests
    {
        [Test]
        public async Task ShouldRun()
        {
            // Given
            using var host = Program.CreateHostBuilder()
                .ConfigureWebHost(b => b.UseTestServer())
                .Build();

            // When
            await host.StartAsync();

            var client = host.Services
                .GetRequiredService<IServer>()
                .ShouldBeOfType<TestServer>()
                .CreateClient();
            var response = await client.GetAsync("/");

            await host.StopAsync();

            // then
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}