using System;

namespace WebApiThrottle
{
    internal static class LocalThrottleCounterExtensions
    {
        public static bool HasExpired(this ThrottleCounter counter, TimeSpan expirationTime)
        { 
            return DateTime.UtcNow >= counter.Timestamp + expirationTime;
        }
    }
}
