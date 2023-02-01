using MxIO.ApiClient;

namespace XtremeIdiots.Portal.RepositoryApiClient
{
    public class RepositoryApiClientOptions : IApiClientOptions
    {
        public string BaseUrl { get; }

        public string ApiKey { get; }

        public string ApiAudience { get; }

        public string? ApiPathPrefix { get; }

        public RepositoryApiClientOptions(string baseUrl, string apiKey, string apiAudience)
        {
            BaseUrl = baseUrl;
            ApiKey = apiKey;
            ApiAudience = apiAudience;
        }

        public RepositoryApiClientOptions(string baseUrl, string apiKey, string apiAudience, string apiPathPrefix) : this(baseUrl, apiKey, apiAudience)
        {
            ApiPathPrefix = apiPathPrefix;
        }
    }
}
