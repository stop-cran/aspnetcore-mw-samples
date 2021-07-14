using System;

namespace SampleApp.Middleware
{
    public class RedisCacheMiddlewareOptions
    {
        public TimeSpan? Ttl { get; set; }
    }
}