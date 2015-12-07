using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace WebApiThrottle.RedisRepository
{
    /// <summary>
    /// Redis cache throttle repository, enabling throttling across multiple machines
    /// in a server farm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This stores two values for each throttle id: "{id}-count" is a counter (which
    /// Redis Cache manages, supporting atomic increments); "{id}-start" stores the
    /// tick count at which the period started.
    /// </para>
    /// <para>
    /// This provider relies on Redis cache to do the parts that are tricky when throttling
    /// in server farms: Redis handles atomic increments of counters across multiple servers,
    /// and it also automatically removes things from the cache when they expire.
    /// </para>
    /// <para>
    /// Note: since servers in a server farm can never have perfectly synchronized clocks
    /// (and in practice, it's quite common for them to be 2 seconds adrift or more) this
    /// provide can produce anomalous-looking results in <see cref="ThrottleCounter.Timestamp"/>.
    /// We store the current time on whichever server creates the throttle counter records,
    /// but if some other server looks up the same record, it's entirely possible that it
    /// will see anomalous situations, such as the start period apparently being in the future.
    /// This means that throttle limits over a 1s time period will likely produce slightly
    /// confusing results in their 'Retry-After' header. In principle, we could avoid this
    /// by asking the Redis cache what it thinks the time is, but
    /// </para>
    /// </remarks>
    public class RedisThrottleRepository : IThrottleRepository
    {
        private IConnectionMultiplexer mux;

        public RedisThrottleRepository(IConnectionMultiplexer multiplexer)
        {
            mux = multiplexer;
        }

        public async Task<ThrottleCounter> IncrementAndGetAsync(string id, TimeSpan expirationTime)
        {
            IDatabase db = mux.GetDatabase();
            string startTimeKey = id + "-start";
            string countKey = id + "-count";

            long startTimeTicks;
            long count;
            try
            {
                RedisValueWithExpiry startTimeWithExpiry = await db.StringGetWithExpiryAsync(startTimeKey);
                RedisValue startTimeString = startTimeWithExpiry.Value;
                if (!(startTimeString.HasValue && long.TryParse(startTimeString, out startTimeTicks)))
                {
                    // Start time record either never existed, or expired, so start
                    // a new period.
                    startTimeTicks = DateTime.UtcNow.Ticks;
                    count = 1;
                    await Task.WhenAll(
                        db.StringSetAsync(countKey, count, expirationTime),
                        db.StringSetAsync(startTimeKey, startTimeTicks, expirationTime));
                }
                else
                {
                    count = await db.StringIncrementAsync(countKey, 1);
                }

                return new ThrottleCounter
                {
                    Timestamp = new DateTime(startTimeTicks, DateTimeKind.Utc),
                    TotalRequests = count
                };

            }
            catch (TimeoutException)
            {
                // We get these with Redis from time to time. Let it go.
                return new ThrottleCounter
                {
                    Timestamp = DateTime.UtcNow,
                    TotalRequests = 1
                };
            }
        }
    }
}
