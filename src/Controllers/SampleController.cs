using System;
using Microsoft.AspNetCore.Mvc;
using SampleApp.Middleware;

namespace SampleApp.Controllers
{
    public class SampleController : Controller
    {
        private readonly Func<DateTime> _nowFactory;

        public SampleController(Func<DateTime> nowFactory)
        {
            _nowFactory = nowFactory;
        }

        [HttpGet]
        [Route("/sample")]
        public string ReturnSomething()
        {
            var feature = HttpContext.Features.Get<IVisitorIdFeature>();

            return $"Current date and time: {_nowFactory()}. Visitor id: {feature?.VisitorId}.";
        }

        [HttpGet]
        [Route("/test_cache")]
        public string CurrentDateTime()
        {
            return $"Current date and time: {_nowFactory()}.";
        }
    }
}