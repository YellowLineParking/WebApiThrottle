using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebApiThrottle.Demo.Helpers
{
    public class CustomThrottlingFilter : ThrottlingFilter
    {
        public CustomThrottlingFilter(ThrottlePolicy policy, IPolicyRepository policyRepository, IThrottleRepository repository, IThrottleLogger logger)
            : base(policy, policyRepository, repository, logger)
        {
            this.QuotaExceededMessage = "API calls quota exceeded! maximum admitted {0} per {1}.";
        }

        protected override string GetClientType(ClaimsIdentity identity, Lazy<IDictionary<string, string[]>> headers)
        {
            string[] result;
            if (headers.Value.TryGetValue("Authorization-Key", out result))
            {
                return result[0];
            }
            return "anon";
        }
    }
}