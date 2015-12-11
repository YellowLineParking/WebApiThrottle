using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace WebApiThrottle
{
    /// <summary>
    /// Throttle action filter
    /// </summary>
    public class ThrottlingFilter : ActionFilterAttribute, IActionFilter
    {
        private IPolicyRepository policyRepository;
        private ThrottlePolicy policy;
        private IThrottleRepository throttleRepository;
        private IThrottleLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlingFilter"/> class.
        /// By default, the <see cref="QuotaExceededResponseCode"/> property 
        /// is set to 429 (Too Many Requests).
        /// </summary>
        public ThrottlingFilter()
        {
            QuotaExceededResponseCode = (HttpStatusCode)429;
            throttleRepository = new CacheRepository();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlingFilter"/> class.
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
        public ThrottlingFilter(ThrottlePolicy policy, IPolicyRepository policyRepository, IThrottleRepository repository, IThrottleLogger logger)
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

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            EnableThrottlingAttribute attrPolicy = null;
            bool applyThrottling = ApplyThrottling(actionContext, out attrPolicy);
            if (applyThrottling)
            {
                // ActionFilters don't get to be async, sadly, so we just have to block
                // the thread until this is done.
                ThrottlingCore.ThrottleDecision decision = ThrottlingCore.ProcessRequest(
                    policyRepository,
                    policy,
                    throttleRepository,
                    actionContext.Request,
                    ThrottlingCore.GetIdentity(actionContext.Request, GetClientType),
                    rateLimitPeriod => attrPolicy.GetLimit(rateLimitPeriod),
                    logger).Result;

                if (decision != null)
                {
                    var message = !string.IsNullOrEmpty(this.QuotaExceededMessage) 
                        ? this.QuotaExceededMessage 
                        : "API calls quota exceeded! maximum admitted {0} per {1}.";

                    var content = this.QuotaExceededContent != null
                        ? this.QuotaExceededContent(decision.RateLimit, decision.RateLimitPeriod)
                        : string.Format(message, decision.RateLimit, decision.RateLimitPeriod);

                    // add status code and retry after x seconds to response
                    actionContext.Response = QuotaExceededResponse(
                        actionContext.Request,
                        string.Format(message, decision.RateLimit, decision.RateLimitPeriod),
                        QuotaExceededResponseCode,
                        decision.RetryAfter);
                }
            }

            base.OnActionExecuting(actionContext);
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

        protected virtual HttpResponseMessage QuotaExceededResponse(HttpRequestMessage request, object content, HttpStatusCode responseCode, string retryAfter)
        {
            var response = request.CreateResponse(responseCode, content);
            response.Headers.Add("Retry-After", new string[] { retryAfter });
            return response;
        }

        private bool ApplyThrottling(HttpActionContext filterContext, out EnableThrottlingAttribute attr)
        {
            var applyThrottling = false;
            attr = null;

            if (filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).Any())
            {
                attr = filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).First();
                applyThrottling = true;
            }

            if (filterContext.ActionDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).Any())
            {
                attr = filterContext.ActionDescriptor.GetCustomAttributes<EnableThrottlingAttribute>(true).First();
                applyThrottling = true;
            }

            // explicit disabled
            if (filterContext.ActionDescriptor.GetCustomAttributes<DisableThrottingAttribute>(true).Any())
            {
                applyThrottling = false;
            }

            return applyThrottling;
        }
    }
}
