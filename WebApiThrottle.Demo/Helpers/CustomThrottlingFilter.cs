namespace WebApiThrottle.Demo.Helpers
{
    public class CustomThrottlingFilter : ThrottlingFilter
    {
        public CustomThrottlingFilter(ThrottlePolicy policy, IPolicyRepository policyRepository, IThrottleRepository repository, IThrottleLogger logger)
            : base(policy, policyRepository, repository, logger)
        {
            this.QuotaExceededMessage = "API calls quota exceeded! maximum admitted {0} per {1}.";
        }

        protected override bool IncludeHeaderInClientKey(string headerName)
        {
            return headerName == "Authorization-Key";
        }
    }
}