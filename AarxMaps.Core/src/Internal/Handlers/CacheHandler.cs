using AarxMaps.Core.Exceptions;

namespace AarxMaps.Core.Internal.Handlers;

internal static class CacheHandler {
    internal static async Task<T> ExecuteOrExceptionAsync<T>(Func<Task<T>> func, string exMsg) {
        try { return await func().ConfigureAwait(false); }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            throw new MapsCacheException(exMsg, inner: ex);
        }
    }
    internal static async Task ExecuteOrExceptionAsync(Func<Task> func, string exMsg) {
        try { await func().ConfigureAwait(false); }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            throw new MapsCacheException(exMsg, inner: ex);
        }
    }

    internal static T ExecuteOrException<T>(Func<T> func, string exMsg) {
        try { return func(); }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            throw new MapsCacheException(exMsg, inner: ex);
        }
    }

    internal static void ExecuteOrException(Action action, string exMsg) {
        try { action(); }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            throw new MapsCacheException(exMsg, inner: ex);
        }
    }
}