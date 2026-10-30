---
sidebar_position: 2
title: Quick start
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Quick start

Register AarxMaps, add a module, inject its service and make one call. The examples are ASP.NET Core minimal APIs; a console version is at the end.

## Store the API key

Run this in the project folder. `AddAarxMaps` reads the key from the `AarxMaps` configuration section below.

```bash
dotnet user-secrets init
dotnet user-secrets set "AarxMaps:ApiKey" "YOUR_API_KEY"
```

## Register and call

<Tabs groupId="package">
<TabItem value="places" label="Places">

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Searching;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAarxMaps(o =>
    builder.Configuration.GetSection("AarxMaps").Bind(o));
builder.Services.AddAarxMapsPlaces();

var app = builder.Build();

app.MapGet("/coffee", async (IPlacesService places, CancellationToken ct) => {
    var request = new TextRequestBuilder()
        .WithTextQuery("coffee in Seattle")
        .WithPageSize(5)
        .Build();

    var result = await places.Search.SearchByTextAsync(request, ct);
    return result.Places.Select(p => new { p.PlaceId, p.DisplayName });
});

app.Run();
```

</TabItem>
<TabItem value="routes" label="Routes">

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Routes.Common.Extensions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAarxMaps(o =>
    builder.Configuration.GetSection("AarxMaps").Bind(o));
builder.Services.AddAarxMapsRoutes();

var app = builder.Build();

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

</TabItem>
</Tabs>

In a minimal API, a `CancellationToken` parameter is bound to the request's abort token, so the Google call stops when the client disconnects.

## Services you can inject

Each module registers an entry service that exposes its features as properties (`places.Search`, `routes.Directions`), and each feature service on its own. Inject whichever reads better.

| Module | Entry service | Feature services |
| --- | --- | --- |
| Places | `IPlacesService` | `IAutocompleteService`, `ISearchService`, `IDetailsService` |
| Routes | `IRoutesService` | `IDirectionsService`, `IDistanceMatrixService` |

## Without a host

In a console app or a test, build the container yourself:

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Places.Services;
using Microsoft.Extensions.DependencyInjection;

var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")!;

var services = new ServiceCollection()
    .AddAarxMaps(o => o.ApiKey = apiKey)
    .AddAarxMapsPlaces();

await using var provider = services.BuildServiceProvider();
var places = provider.GetRequiredService<IPlacesService>();
```

Without a host, the configuration is validated the first time a service is resolved instead of at startup.

## Next steps

- [Configuration and registration](../core/configuration.md): language, region, timeout and the registration rules.
- [Caching](../core/caching.md): turn on response caching.
- [Errors](../core/errors.md): what each exception means.
