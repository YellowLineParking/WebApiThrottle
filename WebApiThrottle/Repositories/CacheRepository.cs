using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace WebApiThrottle
{
    /// <summary>
    /// Stores throttle metrics in asp.net cache
    /// </summary>
    public class CacheRepository : IThrottleRepository
    {
        /// <summary>
        /// Insert or update
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="throttleCounter">
        /// The throttle Counter.
        /// </param>
        /// <param name="expirationTime">
        /// The expiration Time.
        /// </param>
        public void Save(string id, ThrottleCounter throttleCounter, TimeSpan expirationTime)
        {
            if (HttpContext.Current.Cache[id] != null)
            {
                HttpContext.Current.Cache[id] = throttleCounter;
            }
            else
            {
                HttpContext.Current.Cache.Add(
                    id,
                    throttleCounter,
                    null,
                    Cache.NoAbsoluteExpiration,
                    expirationTime,
                    CacheItemPriority.Low,
                    null);
            }
        }

        public ThrottleCounter? FirstOrDefault(string id)
        {
            return (ThrottleCounter?)HttpContext.Current.Cache[id];
        }
    }
}
