---
sidebar_position: 3
title: Errors
---

# Errors

Every exception the SDK throws derives from `MapsApiException` (`AarxMaps.Core.Exceptions`). The subtype tells you what went wrong, and the base class carries Google's status code and error body.

## Example

```csharp title="PlaceLookup.cs"
using AarxMaps.Core.Exceptions;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Details;
using Microsoft.Extensions.Logging;

public sealed class PlaceLookup(
    IPlacesService places, ILogger<PlaceLookup> logger
) {
    public async Task<PlaceDetails?> FindAsync(string id, CancellationToken ct) {
        var request = new DetailsRequestBuilder().WithPlaceId(id).Build();

        try {
            return await places.Details.GetDetailsAsync(request, ct);
        }
        catch (MapsNotFoundException) {
            return null;
        }
        catch (MapsRateLimitException ex) {
            logger.LogWarning("Rate limited. Retry after {Wait}.", ex.RetryAfter);
            throw;
        }
        catch (MapsApiException ex) {
            logger.LogError(ex, "Maps call failed ({Status}): {Body}",
                ex.StatusCode, ex.ResponseBody);
            throw;
        }
    }
}
```

Catch the specific types first: `MapsApiException` matches all of them.

## Exception types

| Exception | When |
| --- | --- |
| `MapsInvalidRequestException` | A builder rejected a value, or Google returned 400 |
| `MapsApiAuthException` | 401 or 403: bad or restricted key, or the API isn't enabled |
| `MapsNotFoundException` | 404, for example an unknown place ID |
| `MapsRateLimitException` | 429: quota or rate limit exceeded |
| `MapsCacheException` | Cache registration mistakes only |
| `MapsApiException` | Any other status, network failure, timeout, or unreadable response |

- Builder errors are thrown by the builder method or `Build()`, before anything is sent, and have no `StatusCode`.
- Runtime cache failures are never thrown; they count as a miss. See [Caching](caching.md).
- A timeout is a plain `MapsApiException`. See [Cancellation and timeouts](cancellation.md) for how to tell it apart.

## Properties

| Property | Type | Holds |
| --- | --- | --- |
| `StatusCode` | `HttpStatusCode?` | Google's HTTP status, or `null` if no response |
| `ResponseBody` | `string?` | Google's error body, first 4,096 characters |
| `RetryAfter` | `TimeSpan?` | Rate limit only: Google's `Retry-After`, if sent |

`ToString()` includes the status code and the body, so a logged exception carries Google's error message.

## Outside the hierarchy

- `OperationCanceledException`: your `CancellationToken` was cancelled.
- `ArgumentNullException`: a `null` request object was passed to a service method.
- `OptionsValidationException`: invalid `MapsConfiguration` or `MapsCachingOptions`, at startup.
- `InvalidOperationException`: `AddAarxMaps` was called twice.

## Retries

The SDK doesn't retry. If you want retries on `MapsRateLimitException` or on 5xx responses, wrap the call in your own policy and wait at least `RetryAfter` when it is set.

## See also

- [Cancellation and timeouts](cancellation.md)
- [Configuration and registration](configuration.md)
