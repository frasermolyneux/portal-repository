using MX.Api.Client.Configuration;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    /// <summary>
    /// Custom options for the Repository API client
    /// </summary>
    public class RepositoryApiClientOptions : ApiClientOptionsBase
    {
        /// <summary>
        /// Gets or sets whether to enable caching for repository operations
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the default page size for collection operations
        /// </summary>
        public int DefaultPageSize { get; set; } = 25;

        /// <summary>
        /// Validates the options
        /// </summary>
        public override void Validate()
        {
            base.Validate();

            if (DefaultPageSize <= 0)
                throw new InvalidOperationException("DefaultPageSize must be greater than 0");
        }
    }
}
