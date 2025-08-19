using MX.Api.Client.Configuration;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    /// <summary>
    /// Builder for Repository API options
    /// </summary>
    public class RepositoryApiOptionsBuilder : ApiClientOptionsBuilder<RepositoryApiClientOptions, RepositoryApiOptionsBuilder>
    {
        /// <summary>
        /// Creates a new instance of the RepositoryApiOptionsBuilder
        /// </summary>
        public RepositoryApiOptionsBuilder() : base() { }

        /// <summary>
        /// Configures the default page size for repository operations
        /// </summary>
        /// <param name="pageSize">The page size</param>
        /// <returns>The builder for chaining</returns>
        public RepositoryApiOptionsBuilder WithDefaultPageSize(int pageSize)
        {
            Options.DefaultPageSize = pageSize;
            return this;
        }

        /// <summary>
        /// Configures whether to enable caching
        /// </summary>
        /// <param name="enableCaching">Whether to enable caching</param>
        /// <returns>The builder for chaining</returns>
        public RepositoryApiOptionsBuilder WithCaching(bool enableCaching = true)
        {
            Options.EnableCaching = enableCaching;
            return this;
        }
    }
}
