using MxIO.ApiClient;

namespace XtremeIdiots.Portal.RepositoryApiClient
{
    public class RepositoryApiClientOptions : ApiClientOptions
    {
        public RepositoryApiClientOptions(string baseUrl, string apiKey, string apiAudience) : base(baseUrl, apiKey, apiAudience)
        {

        }

        public RepositoryApiClientOptions(string baseUrl, string apiKey, string apiAudience, string apiPathPrefix) : base(baseUrl, apiKey, apiAudience, apiPathPrefix)
        {

        }
    }
}
