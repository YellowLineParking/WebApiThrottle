using System;
using System.Web;
using System.Web.Caching;

namespace WebApiThrottle
{
    /// <summary>
    /// Stores throttle metrics in asp.net cache
    /// </summary>
    public class CacheRepository : IThrottleRepository
    {
        private static object sync = new object();

        public ThrottleCounter IncrementAndGet(string id, TimeSpan expirationTime)
        {
            ThrottleCounter currentCounter;
            DateTime now = DateTime.UtcNow;
            lock (sync)
            {
                object currentEntry = HttpContext.Current.Cache[id];
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
                    HttpContext.Current.Cache[id] = currentCounter;
                }
                else
                {
                    currentCounter = new ThrottleCounter
                    {
                        Timestamp = now,
                        TotalRequests = 1
                    };
                    HttpContext.Current.Cache.Add(
                        id,
                        currentCounter,
                        null,
                        now + expirationTime,
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.Low,
                        null);
                } 
            }

            return currentCounter;
        }
    }
}
