using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    public class ThrottlingMiddleware : OwinMiddleware
    {
        private readonly IPolicyRepository policyRepository;
        private readonly ThrottlePolicy policy;
        private readonly IThrottleRepository throttleRepository;
        private readonly IThrottleLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlingMiddleware"/> class. 
        /// By default, the <see cref="QuotaExceededResponseCode"/> property 
        /// is set to 429 (Too Many Requests).
        /// </summary>
        public ThrottlingMiddleware(OwinMiddleware next)
            : base(next)
        {
            QuotaExceededResponseCode = (HttpStatusCode)429;
            throttleRepository = new CacheRepository();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlingMiddleware"/> class.
        /// Persists the policy object in cache using <see cref="IPolicyRepository"/> implementation.
        /// The policy object can be updated by <see cref="ThrottleManager"/> at runtime. 
        /// </summary>
        /// <param name="next">
        /// Next OWIN middleware handler in chain.
        /// </param>
        /// <param name="policy">
        /// The policy.
        /// </param>
        /// <param name="policyRepository">
        /// The policy repository.
        /// </param>
        /// <param name="repository">
        /// The repository.
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        public ThrottlingMiddleware(
            OwinMiddleware next,
            ThrottlePolicy policy,
            IPolicyRepository policyRepository,
            IThrottleRepository repository,
            IThrottleLogger logger)
            : base(next)
        {
            this.throttleRepository = repository;
            this.logger = logger;

            QuotaExceededResponseCode = (HttpStatusCode)429;

            this.policy = policy;
            this.policyRepository = policyRepository;

            if (policyRepository != null)
            {
                policyRepository.Save(ThrottleManager.GetPolicyKey(), policy);
            }
        }

        /// <summary>
        /// Gets or sets a value that will be used as a formatter for the QuotaExceeded response message.
        /// If none specified the default will be: 
        /// API calls quota exceeded! maximum admitted {0} per {1}
        /// </summary>
        public string QuotaExceededMessage { get; set; }

        /// <summary>
        /// Gets or sets the value to return as the HTTP status 
        /// code when a request is rejected because of the
        /// throttling policy. The default value is 429 (Too Many Requests).
        /// </summary>
        public HttpStatusCode QuotaExceededResponseCode { get; set; }

        public override async Task Invoke(IOwinContext context)
        {
            IOwinResponse response = context.Response;
            IOwinRequest request = context.Request;

            ThrottlingCore.ThrottleDecision decision = await ThrottlingCore.ProcessRequest(
                policyRepository,
                policy,
                throttleRepository,
                null,
                ThrottlingCore.GetIdentity(request, GetClientType),
                _ => 0,
                logger);

            if (decision == null)
            {
                await Next.Invoke(context);
                return;
            }

            var message = !string.IsNullOrEmpty(this.QuotaExceededMessage)
                ? this.QuotaExceededMessage
                : "API calls quota exceeded! maximum admitted {0} per {1}.";

            // break execution
            response.OnSendingHeaders(state =>
            {
                var resp = (OwinResponse)state;
                resp.Headers.Add("Retry-After", new string[] { decision.RetryAfter });
                resp.StatusCode = (int)QuotaExceededResponseCode;
                resp.ReasonPhrase = string.Format(message, decision.RateLimit, decision.RateLimitPeriod);
            }, response);

            return;
        }

        /// <summary>
        /// Override to determine the client type key for a request.
        /// </summary>
        /// <param name="identity">
        /// The ClaimsIdentity for the user, or null if there isn't one.
        /// </param>
        /// <param name="headers">
        /// Makes the request headers available.
        /// </param>
        /// <returns>
        /// The ClientTypeKey for this request.
        /// </returns>
        protected virtual string GetClientType(
            ClaimsIdentity identity,
            Lazy<IDictionary<string, string[]>> headers)
        {
            return ThrottlingCore.DefaultClientTypeFinder(identity, headers);
        }
    }
}
