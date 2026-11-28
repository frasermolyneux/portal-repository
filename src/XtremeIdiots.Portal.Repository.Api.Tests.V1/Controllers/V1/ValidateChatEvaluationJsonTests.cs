using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using Xunit;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed class ValidateChatEvaluationJsonTests
{
    [Fact]
    public async Task UnlimitedHostUsesNormalThresholdWithoutFallbackLimit()
    {
        var (context, actionContext) = CreateContext(null, new NonSeekableStream("{}"));
        context.HttpContext.Request.ContentLength = 30_000_001;
        var called = false;
        await new ValidateChatEvaluationJsonAttribute().OnResourceExecutionAsync(context, () =>
        {
            called = true;
            var buffered = Assert.IsType<FileBufferingReadStream>(context.HttpContext.Request.Body);
            Assert.Equal(30 * 1024, buffered.MemoryThreshold);
            return Task.FromResult(new ResourceExecutedContext(actionContext, []));
        });
        Assert.True(called);
        Assert.Null(context.Result);
        await context.HttpContext.Request.Body.DisposeAsync();
    }

    [Fact]
    public async Task HostLimitReturns413ForKnownLengthAndStreamingOverflow()
    {
        foreach (var knownLength in new[] { true, false })
        {
            var (context, actionContext) = CreateContext(1, new NonSeekableStream("{}"));
            context.HttpContext.Request.ContentLength = knownLength ? 2 : null;
            await new ValidateChatEvaluationJsonAttribute().OnResourceExecutionAsync(context,
                () => Task.FromResult(new ResourceExecutedContext(actionContext, [])));
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
            await context.HttpContext.Request.Body.DisposeAsync();
        }
    }

    [Fact]
    public async Task UnrelatedReadFailureIsNotReportedAsSizeOverflow()
    {
        var (context, actionContext) = CreateContext(1024, new NonSeekableStream("{}", fail: true));
        var exception = await Assert.ThrowsAsync<IOException>(() => new ValidateChatEvaluationJsonAttribute()
            .OnResourceExecutionAsync(context, () => Task.FromResult(new ResourceExecutedContext(actionContext, []))));
        Assert.Equal("Read failed", exception.Message);
        await context.HttpContext.Request.Body.DisposeAsync();
    }

    private static (ResourceExecutingContext Context, ActionContext ActionContext) CreateContext(long? limit, Stream body)
    {
        var http = new DefaultHttpContext();
        var feature = new Mock<IHttpMaxRequestBodySizeFeature>();
        feature.SetupGet(item => item.MaxRequestBodySize).Returns(limit);
        http.Features.Set(feature.Object);
        http.Request.Body = body;
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return (new ResourceExecutingContext(actionContext, [], new List<IValueProviderFactory>()), actionContext);
    }

    private sealed class NonSeekableStream(string json, bool fail = false) : Stream
    {
        private readonly MemoryStream inner = new(Encoding.UTF8.GetBytes(json));
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) =>
            fail ? throw new IOException("Read failed") : inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            fail ? ValueTask.FromException<int>(new IOException("Read failed")) : inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) { inner.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
