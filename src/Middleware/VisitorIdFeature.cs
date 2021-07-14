using System;
using System.Linq;

namespace SampleApp.Middleware
{
    public class VisitorIdFeature : IVisitorIdFeature
    {
        public VisitorIdFeature()
        {
            VisitorId = GenerateVisitorId();
            IsFirstTimeVisitor = true;
        }

        public VisitorIdFeature(string visitorId)
        {
            VisitorId = visitorId;
        }

        public string VisitorId { get; }
        public bool IsFirstTimeVisitor { get; }

        private static string GenerateVisitorId()
        {
            var r = new Random();

            return new string(Enumerable.Range(0, 10).Select(_ => r.NextAlphaNumericChar()).ToArray());
        }
    }

    public static class RandomExtensions
    {
        public static char NextAlphaNumericChar(this Random random)
        {
            return random.Next(3) switch
            {
                0 => (char) ('0' + random.Next('9' - '0' + 1)),
                1 => (char) ('a' + random.Next('z' - 'a' + 1)),
                2 => (char) ('A' + random.Next('Z' - 'A' + 1)),
                _ => '_'
            };
        }
    }
}