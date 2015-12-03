using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Stors throttle metrics in runtime cache, intented for owin self host.
    /// </summary>
    public class MemoryCacheRepository : IThrottleRepository
    {
        ObjectCache memCache = MemoryCache.Default;

        /// <summary>
        /// Insert or update
        /// </summary>
        public void Save(string id, ThrottleCounter throttleCounter, TimeSpan expirationTime)
        {
            if (memCache[id] != null)
            {
                memCache[id] = throttleCounter;
            }
            else
            {
                memCache.Add(
                    id,
                    throttleCounter, new CacheItemPolicy()
                    {
                        SlidingExpiration = expirationTime
                    });
            }
        }

        public ThrottleCounter? FirstOrDefault(string id)
        {
            return (ThrottleCounter?)memCache[id];
        }
    }
}
