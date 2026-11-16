using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V2;

[Trait("Category", "Integration")]
public class HostLifetimeRegressionTests
{
    [Fact]
    public async Task RunAsync_StartsHostAndRootReturnsOk()
    {
        var hostedService = new TrackingHostedService();
        var factory = new LifecycleWebApplicationFactory(hostedService);

        try
        {
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/");

            Assert.True(hostedService.Started);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisposeAsync_StopsHostedServices()
    {
        var hostedService = new TrackingHostedService();
        var factory = new LifecycleWebApplicationFactory(hostedService);

        using (factory.CreateClient())
        {
            Assert.True(hostedService.Started);
        }

        await factory.DisposeAsync();

        Assert.True(hostedService.Stopped);
    }

    [Fact]
    public void RunAsync_PropagatesHostedServiceStartupFailure()
    {
        const string failureMessage = "Synthetic startup failure.";
        var hostedService = new FailingHostedService(failureMessage);
        using var factory = new LifecycleWebApplicationFactory(hostedService);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.True(hostedService.StartAttempted);
        Assert.Contains(failureMessage, exception.ToString());
    }

    private sealed class LifecycleWebApplicationFactory(IHostedService hostedService) : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => services.AddSingleton(hostedService));
        }
    }

    private sealed class TrackingHostedService : IHostedService
    {
        public bool Started { get; private set; }

        public bool Stopped { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stopped = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHostedService(string failureMessage) : IHostedService
    {
        public bool StartAttempted { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartAttempted = true;
            return Task.FromException(new InvalidOperationException(failureMessage));
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
