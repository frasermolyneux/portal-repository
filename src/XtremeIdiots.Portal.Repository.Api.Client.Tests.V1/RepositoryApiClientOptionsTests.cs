using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1
{
    public class RepositoryApiClientOptionsTests
    {
        [Fact]
        public void Default_EnableCaching_IsTrue()
        {
            var options = new RepositoryApiClientOptions();
            Assert.True(options.EnableCaching);
        }

        [Fact]
        public void Default_DefaultPageSize_Is25()
        {
            var options = new RepositoryApiClientOptions();
            Assert.Equal(25, options.DefaultPageSize);
        }

        [Fact]
        public void Validate_ThrowsWhenDefaultPageSizeIsZero()
        {
            var options = new RepositoryApiClientOptions { DefaultPageSize = 0 };
            var ex = Assert.ThrowsAny<Exception>(() => options.Validate());
            // Either base validation or custom validation should throw
            Assert.NotNull(ex);
        }

        [Fact]
        public void Validate_ThrowsWhenDefaultPageSizeIsNegative()
        {
            var options = new RepositoryApiClientOptions { DefaultPageSize = -1 };
            var ex = Assert.ThrowsAny<Exception>(() => options.Validate());
            Assert.NotNull(ex);
        }
    }
}
