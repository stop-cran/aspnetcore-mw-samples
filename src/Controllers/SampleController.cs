using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SampleApp.Middleware;

namespace SampleApp.Controllers
{
    public class SampleController : Controller
    {
        private readonly ISystemClock _systemClock;

        public SampleController(ISystemClock systemClock)
        {
            _systemClock = systemClock;
        }

        [HttpGet]
        [Route("/sample")]
        public string ReturnSomething()
        {
            var feature = HttpContext.Features.Get<IVisitorIdFeature>();

            return $"Current date and time: {_systemClock.UtcNow.DateTime.ToLocalTime()}. Visitor id: {feature?.VisitorId}.";
        }

        [HttpGet]
        [Route("/test_cache")]
        public string CurrentDateTime()
        {
            return $"Current date and time: {_systemClock.UtcNow.DateTime.ToLocalTime()}.";
        }
    }
}