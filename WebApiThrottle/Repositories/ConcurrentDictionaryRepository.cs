using System;
using System.Collections.Concurrent;

namespace WebApiThrottle
{
    /// <summary>
    /// Stores throttle metrics in a thread safe dictionary, has no clean-up mechanism, expired counters are deleted on renewal
    /// </summary>
    public class ConcurrentDictionaryRepository : IThrottleRepository
    {
        private static ConcurrentDictionary<string, ThrottleCounter> cache = new ConcurrentDictionary<string, ThrottleCounter>();

        public ThrottleCounter IncrementAndGet(string id, TimeSpan expirationTime)
        {
            ThrottleCounter item = cache.AddOrUpdate(
                id,
                _ =>
                new ThrottleCounter
                {
                    Timestamp = DateTime.UtcNow,
                    TotalRequests = 1
                },
                (_, entry) =>
                {
                    if (entry.HasExpired(expirationTime))
                    {
                        entry.Timestamp = DateTime.UtcNow;
                        entry.TotalRequests = 1;
                    }
                    else
                    {
                        entry.TotalRequests += 1;
                    }
                    return entry;
                });

            return item;
        }
    }
}
