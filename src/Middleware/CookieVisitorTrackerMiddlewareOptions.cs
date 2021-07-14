using System;

namespace SampleApp.Middleware
{
    public class CookieVisitorTrackerMiddlewareOptions
    {
        public string CookieKey { get; set; }
        public TimeSpan MaxAge { get; set; }
    }
}