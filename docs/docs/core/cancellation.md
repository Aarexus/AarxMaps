---
sidebar_position: 4
title: Cancellation and timeouts
---

# Cancellation and timeouts

Every async method takes a `CancellationToken`, and `RequestTimeout` limits each HTTP call. They end a call with different exceptions, so you can tell a caller who gave up from a slow Google response.

## Example

```csharp title="CoffeeFinder.cs"
using AarxMaps.Core.Exceptions;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Searching;

public sealed class CoffeeFinder(IPlacesService places) {
    public async Task<TextSearchResult?> FindAsync(CancellationToken ct) {
        var request = new TextRequestBuilder()
            .WithTextQuery("coffee in Seattle")
            .Build();

        try {
            return await places.Search.SearchByTextAsync(request, ct);
        }
        catch (MapsApiException ex)
            when (ex.InnerException is OperationCanceledException) {
            // RequestTimeout expired: Google was too slow, ct is not cancelled.
            return null;
        }
        // If ct is cancelled, OperationCanceledException reaches the caller.
    }
}
```

## What you get

| Cause | Exception |
| --- | --- |
| Your token is cancelled | `OperationCanceledException` |
| `RequestTimeout` expires | `MapsApiException` |

A timeout is a `MapsApiException` with no `StatusCode` whose `InnerException` is an `OperationCanceledException`.

## The token

- Service methods default `ct` to `CancellationToken.None`. Always pass one.
- In ASP.NET Core, add a `CancellationToken` parameter to the endpoint or action; it is cancelled when the client disconnects. In a `BackgroundService`, pass the `stoppingToken`.
- For your own deadline, link a token and cancel it after a delay. When it fires, you get `OperationCanceledException`, not a timeout:

```csharp
using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
deadline.CancelAfter(TimeSpan.FromSeconds(5));

var result = await places.Search.SearchByTextAsync(request, deadline.Token);
```

## The timeout

- `RequestTimeout` (default 30 s) is the HTTP client's timeout, applied to each request separately. A method that sends several requests can take longer in total.
- For calls whose response is read in one piece, it covers sending the request and reading the whole body.
- For streamed responses (the distance matrix, and `SendStreamAsync` / `SendAndEnumerateAsync` on [custom endpoints](custom-endpoints.md)) it stops once the response headers arrive. Reading the body is limited only by your token.
- `Timeout.InfiniteTimeSpan` turns it off, leaving only your token. See [Configuration](configuration.md).

## See also

- [Errors](errors.md)
- [Configuration and registration](configuration.md)
