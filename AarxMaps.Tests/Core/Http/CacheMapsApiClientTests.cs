using System.Net;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Caching.KeyStrategy;
using AarxMaps.Core.Internal.Caching.Store;
using AarxMaps.Core.Internal.Http;
using AarxMaps.Core.Models;
using AarxMaps.Tests.Core.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AarxMaps.Tests.Core.Http;

[Trait("Package", "Core")]
[Trait("Module", "Http")]
[Trait("Type", "CacheMapsApiClient")]
public class CacheMapsApiClientTests {
    private const string Key = "aarx:v1:KEY";
    private const string KeyFailedMessage = "Maps cache key generation failed; bypassing the cache.";
    private const string ReadFailedMessage = "Maps cache read failed; falling back to the API.";
    private const string WriteFailedMessage = "Maps cache write failed; returning the response uncached.";

    public sealed class Sample { public int Id { get; set; } }

    private sealed class Fixture {
        public ICacheKeyStrategy Keys { get; } = Substitute.For<ICacheKeyStrategy>();
        public ICacheStore Cache { get; } = Substitute.For<ICacheStore>();
        public IMapsApiClient Inner { get; } = Substitute.For<IMapsApiClient>();
        public FakeLogger<CacheMapsApiClient> Logger { get; } = new();
        public CacheMapsApiClient Sut { get; }
        public MapsApiRequest Request { get; } = TestRequests.Request();
        public Sample Api { get; } = new() { Id = 42 };

        public Fixture() {
            // Default world: key ok, cache miss, API returns Api, write ok.
            Keys.GenerateCacheKey<Sample>(Arg.Any<MapsApiRequest>()).Returns(Key);
            Cache.GetAsync<Sample>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Sample?>(null));
            Cache.SetAsync(Arg.Any<string>(), Arg.Any<Sample>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            Inner.SendAsync<Sample>(Arg.Any<MapsApiRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Api));
            Sut = new CacheMapsApiClient(Keys, Cache, Logger, Inner);
        }

        public void CacheReadReturns(Sample? value)
            => Cache.GetAsync<Sample>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(value));
        public void CacheReadFails(Exception ex)
            => Cache.GetAsync<Sample>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Sample?>(ex));
        public void CacheWriteFails(Exception ex)
            => Cache.SetAsync(Arg.Any<string>(), Arg.Any<Sample>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(ex));
        public void ApiFails(Exception ex)
            => Inner.SendAsync<Sample>(Arg.Any<MapsApiRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Sample>(ex));
    }

    private static void AssertSingleWarning(FakeLogger<CacheMapsApiClient> logger, string message, Exception cause) {
        var records = logger.Collector.GetSnapshot();
        records.Should().ContainSingle();
        records[0].Level.Should().Be(LogLevel.Warning);
        records[0].Message.Should().Be(message);
        records[0].Exception.Should().BeSameAs(cause);
    }

    private static Exception MakeApiException(string kind) => kind switch {
        "api" => new MapsApiException("api failed"),
        "auth" => new MapsApiAuthException("auth failed", HttpStatusCode.Unauthorized, null),
        "notfound" => new MapsNotFoundException("nf", HttpStatusCode.NotFound, null),
        "ratelimit" => new MapsRateLimitException("rl", HttpStatusCode.TooManyRequests, null),
        "invalid" => new MapsInvalidRequestException("bad", HttpStatusCode.BadRequest, null),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    // ===================== HIT / MISS / POPULATE =====================
    /*A value-type T makes every cache miss look like a hit. 
    The API is never called and nothing is ever stored. 
    For your reference-type responses it never happens.
    Low priotity -> Packages called for reference-type
    [Fact]
    public async Task SendAsync_ValueTypeCacheMiss_CallsApi() {
        var keys = Substitute.For<ICacheKeyStrategy>();
        var cache = Substitute.For<ICacheStore>();
        var inner = Substitute.For<IMapsApiClient>();
        keys.GenerateCacheKey<int>(Arg.Any<MapsApiRequest>()).Returns(Key);
        cache.GetAsync<int>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0)); // a miss
        inner.SendAsync<int>(Arg.Any<MapsApiRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));
        var sut = new CacheMapsApiClient(keys, cache, new FakeLogger<CacheMapsApiClient>(), inner);

        var result = await sut.SendAsync<int>(TestRequests.Request(), default);

        result.Should().Be(5);
    }*/

    [Fact]
    public async Task SendAsync_CacheHit_ReturnsCachedInstanceWithoutCallingApiOrWriting() {
        var fx = new Fixture();
        var cached = new Sample { Id = 7 };
        fx.CacheReadReturns(cached);

        var result = await fx.Sut.SendAsync<Sample>(fx.Request, default);

        result.Should().BeSameAs(cached);
        fx.Inner.ReceivedCalls().Should().BeEmpty();
        await fx.Cache.DidNotReceiveWithAnyArgs().SetAsync<Sample>(default!, default!, default);
        fx.Logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_CacheMiss_CallsApiOnceStoresResultAndReturnsIt() {
        var fx = new Fixture();

        var result = await fx.Sut.SendAsync<Sample>(fx.Request, default);

        result.Should().BeSameAs(fx.Api);
        await fx.Inner.Received(1).SendAsync<Sample>(fx.Request, Arg.Any<CancellationToken>());
        await fx.Cache.Received(1).SetAsync(Key, fx.Api, Arg.Any<CancellationToken>());
        fx.Logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_CacheMiss_RunsKeyGenerationThenReadThenApiThenWrite_InThatOrder() {
        var fx = new Fixture();
        var ct = CancellationToken.None;

        await fx.Sut.SendAsync<Sample>(fx.Request, ct);

        Received.InOrder(() => {
            fx.Keys.GenerateCacheKey<Sample>(fx.Request);
            fx.Cache.GetAsync<Sample>(Key, ct);
            fx.Inner.SendAsync<Sample>(fx.Request, ct);
            fx.Cache.SetAsync(Key, fx.Api, ct);
        });
    }

    [Fact]
    public async Task SendAsync_Always_GeneratesTheKeyForTheRequestedTypeOnceAndUsesItForReadAndWrite() {
        var fx = new Fixture();

        await fx.Sut.SendAsync<Sample>(fx.Request, default);

        fx.Keys.Received(1).GenerateCacheKey<Sample>(fx.Request);
        await fx.Cache.Received(1).GetAsync<Sample>(Key, Arg.Any<CancellationToken>());
        await fx.Cache.Received(1).SetAsync(Key, fx.Api, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_Always_PassesTheCallersTokenToReadApiAndWrite() {
        var fx = new Fixture();
        using var cts = new CancellationTokenSource();

        await fx.Sut.SendAsync<Sample>(fx.Request, cts.Token);

        await fx.Cache.Received(1).GetAsync<Sample>(Key, cts.Token);
        await fx.Inner.Received(1).SendAsync<Sample>(fx.Request, cts.Token);
        await fx.Cache.Received(1).SetAsync(Key, fx.Api, cts.Token);
    }

    // ===================== CACHE FAILURES ARE SWALLOWED =====================
    [Fact]
    public async Task SendAsync_KeyGenerationFails_ReturnsApiResponseBypassesCacheAndLogsWarning() {
        var fx = new Fixture();
        var failure = new MapsCacheException("key boom");
        fx.Keys.GenerateCacheKey<Sample>(Arg.Any<MapsApiRequest>()).Throws(failure);

        var result = await fx.Sut.SendAsync<Sample>(fx.Request, default);

        result.Should().BeSameAs(fx.Api);
        fx.Cache.ReceivedCalls().Should().BeEmpty("no key → neither read nor write");
        await fx.Inner.Received(1).SendAsync<Sample>(fx.Request, Arg.Any<CancellationToken>());
        AssertSingleWarning(fx.Logger, KeyFailedMessage, failure);
    }

    [Fact]
    public async Task SendAsync_CacheReadFails_FallsBackToApiStillWritesAndLogsWarning() {
        var fx = new Fixture();
        var failure = new MapsCacheException("read boom");
        fx.CacheReadFails(failure);

        var result = await fx.Sut.SendAsync<Sample>(fx.Request, default);

        result.Should().BeSameAs(fx.Api);
        await fx.Inner.Received(1).SendAsync<Sample>(fx.Request, Arg.Any<CancellationToken>());
        await fx.Cache.Received(1).SetAsync(Key, fx.Api, Arg.Any<CancellationToken>());
        AssertSingleWarning(fx.Logger, ReadFailedMessage, failure);
    }

    [Fact]
    public async Task SendAsync_CacheWriteFails_ReturnsApiResponseAndLogsWarning() {
        var fx = new Fixture();
        var failure = new MapsCacheException("write boom");
        fx.CacheWriteFails(failure);

        var result = await fx.Sut.SendAsync<Sample>(fx.Request, default);

        result.Should().BeSameAs(fx.Api);
        AssertSingleWarning(fx.Logger, WriteFailedMessage, failure);
    }

    [Fact]
    public async Task SendAsync_ReadAndWriteBothFail_ReturnsApiResponseAndLogsBothWarnings() {
        var fx = new Fixture();
        var readFailure = new MapsCacheException("read boom");
        var writeFailure = new MapsCacheException("write boom");
        fx.CacheReadFails(readFailure);
        fx.CacheWriteFails(writeFailure);

        var result = await fx.Sut.SendAsync<Sample>(fx.Request, default);

        result.Should().BeSameAs(fx.Api);
        var records = fx.Logger.Collector.GetSnapshot();
        records.Select(r => r.Message).Should().Equal(ReadFailedMessage, WriteFailedMessage);
        records.Select(r => r.Exception).Should().Equal(readFailure, writeFailure);
        records.Should().OnlyContain(r => r.Level == LogLevel.Warning);
    }

    // ===================== API ERRORS PROPAGATE AND ARE NEVER CACHED =====================
    [Theory]
    [InlineData("api")]
    [InlineData("auth")]
    [InlineData("notfound")]
    [InlineData("ratelimit")]
    [InlineData("invalid")]
    public async Task SendAsync_ApiThrows_PropagatesSameExceptionAndNeverWrites(string kind) {
        var fx = new Fixture();
        var apiFailure = MakeApiException(kind);
        fx.ApiFails(apiFailure);

        var act = () => fx.Sut.SendAsync<Sample>(fx.Request, default);

        (await act.Should().ThrowAsync<MapsApiException>()).Which.Should().BeSameAs(apiFailure);
        await fx.Cache.DidNotReceiveWithAnyArgs().SetAsync<Sample>(default!, default!, default);
        fx.Logger.Collector.Count.Should().Be(0, "API failures are not cache problems");
    }

    // ===================== CANCELLATION IS NEVER SWALLOWED =====================
    [Fact]
    public async Task SendAsync_CancelledDuringCacheRead_PropagatesWithoutCallingApiOrLogging() {
        var fx = new Fixture();
        var cancelled = new OperationCanceledException("read cancelled");
        fx.CacheReadFails(cancelled);

        var act = () => fx.Sut.SendAsync<Sample>(fx.Request, default);

        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(cancelled);
        fx.Inner.ReceivedCalls().Should().BeEmpty();
        await fx.Cache.DidNotReceiveWithAnyArgs().SetAsync<Sample>(default!, default!, default);
        fx.Logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_CancelledDuringApiCall_PropagatesWithoutWritingOrLogging() {
        var fx = new Fixture();
        var cancelled = new OperationCanceledException("api cancelled");
        fx.ApiFails(cancelled);

        var act = () => fx.Sut.SendAsync<Sample>(fx.Request, default);

        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(cancelled);
        await fx.Cache.DidNotReceiveWithAnyArgs().SetAsync<Sample>(default!, default!, default);
        fx.Logger.Collector.Count.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_CancelledDuringCacheWrite_PropagatesWithoutLogging() {
        var fx = new Fixture();
        var cancelled = new OperationCanceledException("write cancelled");
        fx.CacheWriteFails(cancelled);

        var act = () => fx.Sut.SendAsync<Sample>(fx.Request, default);

        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(cancelled);
        fx.Logger.Collector.Count.Should().Be(0);
    }

    // ===================== STREAMS BYPASS THE CACHE =====================
    [Fact]
    public async Task SendStreamAsync_Always_ForwardsRequestAndTokenAndReturnsTheInnerResponseUntouched() {
        var fx = new Fixture();
        using var cts = new CancellationTokenSource();
        var expected = new StreamResponse(new MemoryStream(), new HttpResponseMessage(HttpStatusCode.OK));
        fx.Inner.SendStreamAsync(fx.Request, cts.Token).Returns(Task.FromResult(expected));

        var actual = await fx.Sut.SendStreamAsync(fx.Request, cts.Token);

        actual.Should().BeSameAs(expected);
        await fx.Inner.Received(1).SendStreamAsync(fx.Request, cts.Token);
        fx.Keys.ReceivedCalls().Should().BeEmpty();
        fx.Cache.ReceivedCalls().Should().BeEmpty();
        fx.Logger.Collector.Count.Should().Be(0);
        await expected.DisposeAsync();
    }

    [Fact]
    public async Task SendStreamAsync_InnerFails_PropagatesSameExceptionWithoutTouchingCache() {
        var fx = new Fixture();
        var failure = new MapsNotFoundException("nf", HttpStatusCode.NotFound, null);
        fx.Inner.SendStreamAsync(Arg.Any<MapsApiRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StreamResponse>(failure));

        var act = () => fx.Sut.SendStreamAsync(fx.Request, default);

        (await act.Should().ThrowAsync<MapsNotFoundException>()).Which.Should().BeSameAs(failure);
        fx.Keys.ReceivedCalls().Should().BeEmpty();
        fx.Cache.ReceivedCalls().Should().BeEmpty();
    }
}
