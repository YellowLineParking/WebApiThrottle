using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebApiThrottle
{
    /// <summary>
    /// Throttle message handler
    /// </summary>
    public class ThrottlingHandler : DelegatingHandler
    {
        private IPolicyRepository policyRepository;
        private ThrottlePolicy policy;
        private IThrottleRepository throttleRepository;
        private IThrottleLogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlingHandler"/> class. 
        /// By default, the <see cref="QuotaExceededResponseCode"/> property 
        /// is set to 429 (Too Many Requests).
        /// </summary>
        public ThrottlingHandler()
        {
            QuotaExceededResponseCode = (HttpStatusCode)429;
            throttleRepository = new CacheRepository();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlingHandler"/> class.
        /// Persists the policy object in cache using <see cref="IPolicyRepository"/> implementation.
        /// The policy object can be updated by <see cref="ThrottleManager"/> at runtime. 
        /// </summary>
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
        public ThrottlingHandler(ThrottlePolicy policy, IPolicyRepository policyRepository, IThrottleRepository repository, IThrottleLogger logger)
        {
            this.throttleRepository = repository;
            Logger = logger;

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
        /// Gets or sets a value that will be used as a formatter for the QuotaExceeded response message.
        /// If none specified the default will be: 
        /// API calls quota exceeded! maximum admitted {0} per {1}
        /// </summary>
        public Func<long, RateLimitPeriod, object> QuotaExceededContent { get; set; }

        /// <summary>
        /// Gets or sets the value to return as the HTTP status 
        /// code when a request is rejected because of the
        /// throttling policy. The default value is 429 (Too Many Requests).
        /// </summary>
        public HttpStatusCode QuotaExceededResponseCode { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ThrottlingCore.ThrottleDecision decision = await ThrottlingCore.ProcessRequest(
                policyRepository,
                policy,
                throttleRepository,
                request,
                ThrottlingCore.GetIdentity(request, IncludeHeaderInClientKey),
                _ => 0,
                Logger);

            if (decision == null)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var message = !string.IsNullOrEmpty(this.QuotaExceededMessage) 
                ? this.QuotaExceededMessage 
                : "API calls quota exceeded! maximum admitted {0} per {1}.";

            var content = this.QuotaExceededContent != null
                ? this.QuotaExceededContent(decision.RateLimit, decision.RateLimitPeriod)
                : string.Format(message, decision.RateLimit, decision.RateLimitPeriod);

            // break execution
            return QuotaExceededResponse(
                request,
                content,
                QuotaExceededResponseCode,
                decision.RetryAfter);
        }

        protected virtual bool IncludeHeaderInClientKey(string headerName)
        {
            return ThrottlingCore.IncludeDefaultClientKeyHeaders(headerName);
        }


        protected virtual HttpResponseMessage QuotaExceededResponse(HttpRequestMessage request, object content, HttpStatusCode responseCode, string retryAfter)
        {
            var response = request.CreateResponse(responseCode, content);
            response.Headers.Add("Retry-After", new string[] { retryAfter });
            return response;
        }
    }
}
