---
sidebar_position: 6
title: Custom endpoints
---

# Custom endpoints

`IMapsApiClient` (`AarxMaps.Core.Abstractions`) is the gateway Places and Routes use to reach Google. Inject it to call any other Google Maps endpoint with the same API key, language header, timeout, error mapping and cache.

## Example

Typed result from a GET endpoint that takes the key in the query string:

```csharp title="ElevationClient.cs"
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Enums;
using AarxMaps.Core.Models;

public sealed record ElevationResponse(
    string Status, IReadOnlyList<ElevationResult> Results);
public sealed record ElevationResult(double Elevation, double Resolution);

public sealed class ElevationClient(IMapsApiClient client) {
    public async Task<double?> GetElevationAsync(CancellationToken ct) {
        var request = new MapsApiRequest {
            BaseUrl = new Uri("https://maps.googleapis.com/maps/api/"),
            Endpoint = "elevation/json",
            Query = new Dictionary<string, string> {
                ["locations"] = "39.7391536,-104.9847034",
            },
            ApiKeyLocation = ApiKeyLocation.Query,
        };

        var response = await client.SendAsync<ElevationResponse>(request, ct);

        // This API reports errors inside a 200 response: check Status.
        return response.Status == "OK" ? response.Results[0].Elevation : null;
    }
}
```

Raw JSON from a POST endpoint, read with the `JsonExtensions` helpers:

```csharp title="AddressValidator.cs"
using System.Text.Json;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Models;

public sealed class AddressValidator(IMapsApiClient client) {
    public async Task<string?> GetFormattedAddressAsync(
        string address, CancellationToken ct
    ) {
        var request = new MapsApiRequest {
            Method = HttpMethod.Post,
            BaseUrl = new Uri("https://addressvalidation.googleapis.com/"),
            Endpoint = "v1:validateAddress",
            Payload = new {
                Address = new { AddressLines = new[] { address } },
            },
        };

        using var json = await client.SendAsync<JsonDocument>(request, ct);

        return json.RootElement
            .GetObject("result")?
            .GetObject("address")?
            .GetStringValue("formattedAddress");
    }
}
```

## Request

`MapsApiRequest` (`AarxMaps.Core.Models`), set with an object initializer:

| Property | Default | Notes |
| --- | --- | --- |
| `Method` | `GET` | Any `HttpMethod` |
| `BaseUrl` | required | For example `https://routes.googleapis.com/` |
| `Endpoint` | required | Path after `BaseUrl` |
| `Headers` | `null` | Extra headers |
| `Query` | `null` | Query parameters, URL-escaped |
| `Payload` | `null` | Sent as a JSON body |
| `ApiKeyLocation` | `Header` | Or `Query` |
| `Cacheable` | `true` | `false` skips the cache |

- **URL:** `BaseUrl` and `Endpoint` are joined with a single `/`, then `Query` is appended.
- **API key:** `Header` sends `X-Goog-Api-Key`; `Query` adds `key=` to the URL for endpoints that only accept it there. The SDK adds the key itself, so never put it in `Headers` or `Query`.
- **Headers:** yours are added to the defaults and replace a default with the same name (`Accept-Language`). New Google APIs require a field mask: add `X-Goog-FieldMask`.
- **Payload:** serialized with camelCase property names, and `null` properties are left out. `Address` above becomes `address`.

## Sending

| Method | Use it for |
| --- | --- |
| `SendAsync<T>` | Most endpoints: buffers and deserializes the body |
| `SendAndEnumerateAsync<T>` | Bodies that are one top-level JSON array |
| `SendStreamAsync` | Raw access to the response stream |

- `SendAsync<T>` reads property names case-insensitively, and numbers sent as strings (Google's int64 fields) bind to numeric properties. An empty body, or one that doesn't fit `T`, throws `MapsApiException`.
- `SendAndEnumerateAsync<T>` (extension in `AarxMaps.Core.Extensions`) yields each array element as it arrives. The request is sent when enumeration starts, and every enumeration sends a new one.
- `SendStreamAsync` returns a `StreamResponse` that you own: dispose it with `await using`, even if you never read it, and read only its `Stream`.

```csharp title="Streaming"
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Models;

public sealed record MatrixElement(
    int OriginIndex, int DestinationIndex, int DistanceMeters);

public static class MatrixReader {
    public static async Task PrintAsync(
        IMapsApiClient client, MapsApiRequest request, CancellationToken ct
    ) {
        var elements = client.SendAndEnumerateAsync<MatrixElement>(request, ct);

        // Elements arrive in no guaranteed order; use their own indices.
        await foreach (var element in elements)
            Console.WriteLine(element);
    }

    public static async Task<string> ReadRawAsync(
        IMapsApiClient client, MapsApiRequest request, CancellationToken ct
    ) {
        // Lower level: you own the response and must dispose it.
        await using var response = await client.SendStreamAsync(request, ct);
        using var reader = new StreamReader(response.Stream);
        return await reader.ReadToEndAsync(ct);
    }
}
```

:::warning[Dispose JsonDocument results]
`SendAsync<JsonDocument>` returns a document you own. Dispose it (`using var json = ...`); with a cache layer, every call still gets its own document, so disposing never affects the cache.
:::

## Errors and caching

- Errors are mapped from the HTTP status, exactly as described in [Errors](errors.md). APIs that report errors inside a 200 body, like the Elevation API above, are not detected: check their status field yourself.
- `SendAsync<T>` results are cached when a cache layer is registered and `Cacheable` is `true`. The streaming methods are never cached.
- The cache key includes the method, URL, query, body and only these headers: `X-Goog-FieldMask`, `Accept-Language` and `Content-Type`. If any other header changes the response, set `Cacheable = false`.
- Set `Cacheable = false` as well for content that Google's terms don't let you cache. See [Caching](caching.md).

## JSON helpers

`JsonExtensions` (`AarxMaps.Core.Extensions`) reads a property from a `JsonElement` and returns `null` (or an empty sequence) when the property is missing or has another JSON type. They never throw.

| Method | Returns |
| --- | --- |
| `GetObject` | `JsonElement?` |
| `GetStringValue` | `string?` |
| `GetInt` | `int?` |
| `GetFloat` | `float?` |
| `GetDoubleValue` | `double?` |
| `GetBool` | `bool?` |
| `GetArray` | `JsonElement.ArrayEnumerator` |
| `GetArrayStringValues` | `IReadOnlyList<string>` |

`GetArrayStringValues` skips array items that aren't strings or are blank. `GetInt` returns `null` for numbers that don't fit an `int`.

## See also

- [Errors](errors.md)
- [Caching](caching.md)
- [Cancellation and timeouts](cancellation.md)
