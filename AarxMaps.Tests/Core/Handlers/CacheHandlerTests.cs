using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Handlers;
using FluentAssertions;

namespace AarxMaps.Tests.Core.Handlers;

[Trait("Package", "Core")]
[Trait("Module", "Handlers")]
[Trait("Type", "CacheHandler")]
public class CacheHandlerTests {
    private const string Msg = "cache operation failed";

    public static TheoryData<Type> FailureTypes => [
        typeof(InvalidOperationException),
        typeof(IOException),
        typeof(TimeoutException),
        typeof(ArgumentException),
        typeof(NotSupportedException),
        typeof(FormatException),
        typeof(KeyNotFoundException),
        typeof(HttpRequestException),
        typeof(System.Text.Json.JsonException)
    ];

    public static TheoryData<Type> CancellationTypes => [
        typeof(OperationCanceledException), typeof(TaskCanceledException)
    ];

    private static Exception Create(Type type) => (Exception)Activator.CreateInstance(type, "boom")!;

    private static void AssertWrapped(MapsCacheException thrown, Exception original) {
        thrown.Message.Should().Be(Msg);
        thrown.InnerException.Should().BeSameAs(original);
    }

    // ===================== ExecuteOrExceptionAsync<T> =====================
    [Fact]
    public async Task ExecuteOrExceptionAsync_WithResult_DelegateSucceeds_ReturnsValueAndRunsOnce() {
        var calls = 0;
        Task<int> func() { calls++; return Task.FromResult(42); }

        var result = await CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        result.Should().Be(42);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteOrExceptionAsync_WithResult_DelegateReturnsNull_ReturnsNull() {
        static Task<string?> func() => Task.FromResult<string?>(null);

        var result = await CacheHandler.ExecuteOrExceptionAsync(func, Msg);
        result.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(FailureTypes))]
    public async Task ExecuteOrExceptionAsync_WithResult_DelegateFaultsAfterAwait_WrapsInMapsCacheException(Type type) {
        var original = Create(type);
        async Task<int> func() { await Task.Yield(); throw original; }

        var act = () => CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        AssertWrapped((await act.Should().ThrowExactlyAsync<MapsCacheException>()).Which, original);
    }

    [Fact]
    public async Task ExecuteOrExceptionAsync_WithResult_DelegateThrowsSynchronously_IsAlsoWrapped() {
        var original = new InvalidOperationException("sync");
        Task<int> func() => throw original;

        var act = () => CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        AssertWrapped((await act.Should().ThrowExactlyAsync<MapsCacheException>()).Which, original);
    }

    [Theory]
    [MemberData(nameof(CancellationTypes))]
    public async Task ExecuteOrExceptionAsync_WithResult_Cancellation_PropagatesUnwrapped(Type type) {
        var original = Create(type);
        async Task<int> func() { await Task.Yield(); throw original; }

        var act = () => CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(original);
    }

    [Fact]
    public async Task ExecuteOrExceptionAsync_WithResult_DelegateObservesCancelledToken_PropagatesTaskCanceledException() {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        async Task<int> func() { await Task.Delay(Timeout.Infinite, cts.Token); return 0; }

        var act = () => CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        var thrown = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        thrown.Should().NotBeOfType<MapsCacheException>();
    }

    // ===================== ExecuteOrExceptionAsync (no result) =====================
    [Fact]
    public async Task ExecuteOrExceptionAsync_NoResult_DelegateSucceeds_CompletesAndRunsOnce() {
        var calls = 0;
        async Task func() { await Task.Yield(); calls++; }

        await CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        calls.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(FailureTypes))]
    public async Task ExecuteOrExceptionAsync_NoResult_DelegateFaults_WrapsInMapsCacheException(Type type) {
        var original = Create(type);
        async Task func() { await Task.Yield(); throw original; }

        var act = () => CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        AssertWrapped((await act.Should().ThrowExactlyAsync<MapsCacheException>()).Which, original);
    }

    [Fact]
    public async Task ExecuteOrExceptionAsync_NoResult_DelegateThrowsSynchronously_IsAlsoWrapped() {
        var original = new InvalidOperationException("sync");
        Task func() => throw original;

        var act = () => CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        AssertWrapped((await act.Should().ThrowExactlyAsync<MapsCacheException>()).Which, original);
    }

    [Theory]
    [MemberData(nameof(CancellationTypes))]
    public async Task ExecuteOrExceptionAsync_NoResult_Cancellation_PropagatesUnwrapped(Type type) {
        var original = Create(type);
        async Task func() { await Task.Yield(); throw original; }

        var act = () => CacheHandler.ExecuteOrExceptionAsync(func, Msg);

        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(original);
    }

    [Fact]
    public async Task ExecuteOrExceptionAsync_NoResult_AlreadyCancelledTask_PropagatesTaskCanceledException() {
        static Task func() => Task.FromCanceled(new CancellationToken(canceled: true));
        var act = () => CacheHandler.ExecuteOrExceptionAsync(func, Msg);
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    // ===================== ExecuteOrException<T> (sync) =====================
    [Fact]
    public void ExecuteOrException_WithResult_DelegateSucceeds_ReturnsValueAndRunsOnce() {
        var calls = 0;
        int func() { calls++; return 7; }

        var result = CacheHandler.ExecuteOrException(func, Msg);

        result.Should().Be(7);
        calls.Should().Be(1);
    }

    [Fact]
    public void ExecuteOrException_WithResult_DelegateReturnsNull_ReturnsNull() {
        static string? func() => null;
        CacheHandler.ExecuteOrException(func, Msg).Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(FailureTypes))]
    public void ExecuteOrException_WithResult_DelegateThrows_WrapsInMapsCacheException(Type type) {
        var original = Create(type);
        int func() => throw original;

        var act = () => CacheHandler.ExecuteOrException(func, Msg);

        AssertWrapped(act.Should().ThrowExactly<MapsCacheException>().Which, original);
    }

    [Theory]
    [MemberData(nameof(CancellationTypes))]
    public void ExecuteOrException_WithResult_Cancellation_PropagatesUnwrapped(Type type) {
        var original = Create(type);
        int func() => throw original;

        var act = () => CacheHandler.ExecuteOrException(func, Msg);

        act.Should().Throw<OperationCanceledException>().Which.Should().BeSameAs(original);
    }

    // ===================== ExecuteOrException (sync, no result) =====================
    [Fact]
    public void ExecuteOrException_NoResult_DelegateSucceeds_CompletesAndRunsOnce() {
        var calls = 0;
        void action() => calls++;

        CacheHandler.ExecuteOrException(action, Msg);

        calls.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(FailureTypes))]
    public void ExecuteOrException_NoResult_DelegateThrows_WrapsInMapsCacheException(Type type) {
        var original = Create(type);
        void action() => throw original;

        var act = () => CacheHandler.ExecuteOrException(action, Msg);

        AssertWrapped(act.Should().ThrowExactly<MapsCacheException>().Which, original);
    }

    [Theory]
    [MemberData(nameof(CancellationTypes))]
    public void ExecuteOrException_NoResult_Cancellation_PropagatesUnwrapped(Type type) {
        var original = Create(type);
        Action action = () => throw original;

        var act = () => CacheHandler.ExecuteOrException(action, Msg);

        act.Should().Throw<OperationCanceledException>().Which.Should().BeSameAs(original);
    }
}