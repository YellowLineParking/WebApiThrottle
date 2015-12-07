using System;

namespace WebApiThrottle
{
    /// <summary>
    /// Reports the initial access time and the numbers of calls made from that point
    /// </summary>
    /// <remarks>
    /// Some throttle store providers also use this to store the current throttle state.
    /// </remarks>
    [Serializable]
    public struct ThrottleCounter
    {
        public DateTime Timestamp { get; set; }

        public long TotalRequests { get; set; }
    }
}
