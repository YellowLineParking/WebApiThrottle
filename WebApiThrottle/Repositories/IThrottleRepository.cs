using System;

namespace WebApiThrottle
{
    /// <summary>
    /// Implement this interface if you want to create a persistent store for the throttle metrics
    /// </summary>
    public interface IThrottleRepository
    {
        /// <summary>
        /// Adds one to the stored value (or creates a new entry with a value of 1
        /// if this is the first call for this id, or if the value previously stored
        /// has expired).
        /// </summary>
        /// <param name="id">
        /// Unique id for this throttle counter.
        /// </param>
        /// <param name="expirationTime">
        /// Time after which to expire this counter.
        /// </param>
        /// <returns>
        /// The current count.
        /// </returns>
        /// <remarks>
        /// The first time you call this for a particular id, it will return 1. Subsequent
        /// calls will return progressively higher values until the entry has been around
        /// as long as (or longer than) the expiration time, at which point it will be as
        /// though this method is being called for the first time.
        /// </remarks>
        ThrottleCounter IncrementAndGet(string id, TimeSpan expirationTime);
    }
}
