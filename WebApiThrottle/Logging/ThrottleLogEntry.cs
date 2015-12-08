using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    [Serializable]
    public class ThrottleLogEntry
    {
        public string RequestId { get; set; }

        public string ClientIp { get; set; }

        public string ClientTypeKey { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public string Endpoint { get; set; }

        public long TotalRequests { get; set; }

        public DateTime StartPeriod { get; set; }

        public long RateLimit { get; set; }

        public string RateLimitPeriod { get; set; }

        public DateTime LogDate { get; set; }

        public object Request { get; set; }
    }
}
