# AarxMaps

Strongly typed .NET SDK for the Google Maps Platform: **Places API (New)** and **Routes API**, with fluent request builders, dependency injection, typed exceptions and optional response caching.

| Package           | What it gives you                                                                  |
| ----------------- | ---------------------------------------------------------------------------------- |
| `AarxMaps.Core`   | Configuration, DI entry point, HTTP client, exceptions, caching, `LatLng`          |
| `AarxMaps.Places` | Autocomplete, Text Search, Nearby Search, Place Details, Photos, Reviews           |
| `AarxMaps.Routes` | Directions (`computeRoutes`) and Distance Matrix (`computeRouteMatrix`)            |

Runs on **.NET 10** and later. MIT licensed.

**Documentation: [aarexus.github.io/AarxMaps](https://aarexus.github.io/AarxMaps/)**

## Requirements

- .NET 10 or later. Apps on .NET 8 or 9 can't install the packages until they upgrade.
- A Google Cloud API key with the APIs you use enabled: **Places API (New)** for `AarxMaps.Places`, **Routes API** for `AarxMaps.Routes`. The legacy Places API is not used.
- Billing enabled on the Google Cloud project. Every call in this SDK is a billed Google request.

## Installation

```bash
dotnet add package AarxMaps.Places   # pulls in AarxMaps.Core
dotnet add package AarxMaps.Routes   # pulls in AarxMaps.Core
```

Install `AarxMaps.Core` on its own only to call Google endpoints directly through `IMapsApiClient`, without a feature module. See [Custom endpoints](https://aarexus.github.io/AarxMaps/docs/core/custom-endpoints/).

## Quick start

Store the API key in user secrets, under the `AarxMaps` section that `AddAarxMaps` binds:

```bash
dotnet user-secrets init
dotnet user-secrets set "AarxMaps:ApiKey" "YOUR_API_KEY"
```

ASP.NET Core (`Program.cs`):

```csharp
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Searching;
using AarxMaps.Routes.Common.Extensions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAarxMaps(o =>
    builder.Configuration.GetSection("AarxMaps").Bind(o));
builder.Services.AddAarxMapsPlaces();
builder.Services.AddAarxMapsRoutes();

var app = builder.Build();

app.MapGet("/coffee", async (IPlacesService places, CancellationToken ct) => {
    var request = new TextRequestBuilder()
        .WithTextQuery("coffee in Seattle")
        .WithPageSize(5)
        .Build();

    var result = await places.Search.SearchByTextAsync(request, ct);
    return result.Places.Select(p => new { p.PlaceId, p.DisplayName });
});

app.MapGet("/route", async (IRoutesService routes, CancellationToken ct) => {
    var spaceNeedle = new LatLng(47.6205, -122.3493);

    var request = new RouteRequestBuilder()
        .From(new WaypointBuilder().FromAddress("Pike Place Market").Build())
        .To(new WaypointBuilder().FromLocation(spaceNeedle).Build())
        .WithTravelMode(TravelMode.Drive)
        .Build();

    var result = await routes.Directions.GetRoutesAsync(request, ct: ct);
    var route = result.Routes
        .FirstOrDefault(r => r.Labels.Contains(RouteLabel.DefaultRoute));

    return route is null
        ? Results.NotFound()
        : Results.Ok(new { route.DistanceMeters, route.Duration });
});

app.Run();
```

For a console app without a host, and the services you can inject, see the [Quick start](https://aarexus.github.io/AarxMaps/docs/getting-started/quick-start/) page.

## Documentation

- **Getting started:** [Introduction](https://aarexus.github.io/AarxMaps/docs/intro/), [Installation](https://aarexus.github.io/AarxMaps/docs/getting-started/installation/), [Quick start](https://aarexus.github.io/AarxMaps/docs/getting-started/quick-start/)
- **Core:** [Configuration and registration](https://aarexus.github.io/AarxMaps/docs/core/configuration/), [Caching](https://aarexus.github.io/AarxMaps/docs/core/caching/), [Errors](https://aarexus.github.io/AarxMaps/docs/core/errors/), [Cancellation and timeouts](https://aarexus.github.io/AarxMaps/docs/core/cancellation/), [Coordinates](https://aarexus.github.io/AarxMaps/docs/core/coordinates/), [Custom endpoints](https://aarexus.github.io/AarxMaps/docs/core/custom-endpoints/)
- **Places:** [Overview](https://aarexus.github.io/AarxMaps/docs/places/overview/), [Search areas](https://aarexus.github.io/AarxMaps/docs/places/search-areas/), [Autocomplete](https://aarexus.github.io/AarxMaps/docs/places/autocomplete/), [Text search](https://aarexus.github.io/AarxMaps/docs/places/text-search/), [Nearby search](https://aarexus.github.io/AarxMaps/docs/places/nearby-search/), [Place details](https://aarexus.github.io/AarxMaps/docs/places/place-details/), [Photos and reviews](https://aarexus.github.io/AarxMaps/docs/places/photos-and-reviews/)
- **Routes:** [Overview](https://aarexus.github.io/AarxMaps/docs/routes/overview/), [Waypoints](https://aarexus.github.io/AarxMaps/docs/routes/waypoints/), [Directions](https://aarexus.github.io/AarxMaps/docs/routes/directions/), [Route results](https://aarexus.github.io/AarxMaps/docs/routes/route-results/), [Distance matrix](https://aarexus.github.io/AarxMaps/docs/routes/distance-matrix/), [Option rules by travel mode](https://aarexus.github.io/AarxMaps/docs/routes/travel-mode-rules/)
- [Known limitations](https://aarexus.github.io/AarxMaps/docs/known-limitations/)
- [Coming from DelgadoMaps](https://aarexus.github.io/AarxMaps/docs/coming-from-delgadomaps/): every renamed type and member, for users of the `D3lg4doMaps.*` packages.

## License

MIT. See [LICENSE](https://github.com/Aarexus/AarxMaps/blob/main/LICENSE).

Not affiliated with or endorsed by Google. Use of Google Maps Platform is subject to [Google's terms of service](https://cloud.google.com/maps-platform/terms).
