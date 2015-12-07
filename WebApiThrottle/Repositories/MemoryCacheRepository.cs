using System;
using System.Runtime.Caching;

namespace WebApiThrottle
{
    /// <summary>
    /// Stors throttle metrics in runtime cache, intented for owin self host.
    /// </summary>
    public class MemoryCacheRepository : IThrottleRepository
    {
        private static object sync = new object();

        ObjectCache memCache = MemoryCache.Default;

        public ThrottleCounter IncrementAndGet(string id, TimeSpan expirationTime)
        {
            ThrottleCounter currentCounter;
            DateTime now = DateTime.UtcNow;
            lock (sync)
            {
                object currentEntry = memCache[id];
                if (currentEntry != null)
                {
                    currentCounter = (ThrottleCounter) currentEntry;
                    if (currentCounter.HasExpired(expirationTime))
                    {
                        currentCounter.Timestamp = now;
                        currentCounter.TotalRequests = 1;
                    }
                    else
                    {
                        currentCounter.TotalRequests += 1;
                    }
                    memCache[id] = currentCounter;
                }
                else
                {
                    currentCounter = new ThrottleCounter
                    {
                        Timestamp = now,
                        TotalRequests = 1
                    };
                    memCache.Add(
                        id,
                        currentCounter,
                        new CacheItemPolicy
                        {
                            AbsoluteExpiration = now + expirationTime
                        });
                
                }
            }

            return currentCounter;
        }
    }
}
