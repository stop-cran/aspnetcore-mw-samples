using System;

namespace SampleApp.Middleware
{
    public class RateLimiterMiddlewareOptions
    {
        public int MaxRequests { get; set; }
        public TimeSpan Interval { get; set; }
        public string RedisKey { get; set; }
    }
}