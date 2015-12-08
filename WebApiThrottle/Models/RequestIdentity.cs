using System;

namespace WebApiThrottle
{
    /// <summary>
    /// Stores the client IP, key and endpoint
    /// </summary>
    [Serializable]
    public class RequestIdentity
    {
        public string ClientIp { get; set; }

        public string ClientTypeKey { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public string Endpoint { get; set; }
    }
}
