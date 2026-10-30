---
sidebar_position: 1
title: Introduction
---

# Introduction

AarxMaps is a strongly typed .NET SDK for Google Maps Platform. You build requests with fluent builders, register the services through dependency injection, and get typed results and typed exceptions back. It runs on .NET 8 and later.

## Packages

| Package | What it covers |
| --- | --- |
| `AarxMaps.Core` | Configuration, registration, HTTP client, errors, caching and coordinates |
| `AarxMaps.Places` | Places API (New): autocomplete, text and nearby search, details, photos and reviews |
| `AarxMaps.Routes` | Routes API: directions and distance matrix |

Places and Routes each depend on Core, so installing either one brings Core with it. Install only the modules you use.

## Example

Find a place by text with Places, then get directions to it with Routes, using the place ID from the first call.

```csharp title="Program.cs"
using AarxMaps.Core.Extensions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Extensions;
using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Searching;
using AarxMaps.Routes.Common.Extensions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services;
using AarxMaps.Routes.Services.Directions;
using Microsoft.Extensions.DependencyInjection;

var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")!;

var services = new ServiceCollection()
    .AddAarxMaps(o => o.ApiKey = apiKey)
    .AddAarxMapsPlaces()
    .AddAarxMapsRoutes();

await using var provider = services.BuildServiceProvider();
var places = provider.GetRequiredService<IPlacesService>();
var routes = provider.GetRequiredService<IRoutesService>();

// In an app, pass the caller's token (for example HttpContext.RequestAborted).
var ct = CancellationToken.None;

// 1. Places: find the destination.
var searchRequest = new TextRequestBuilder()
    .WithTextQuery("Space Needle, Seattle")
    .WithPageSize(1)
    .Build();

var search = await places.Search.SearchByTextAsync(searchRequest, ct);

if (search.Places.Count == 0) {
    Console.WriteLine("No place found.");
    return;
}
var place = search.Places[0];

// 2. Routes: drive there from a coordinate.
var start = new LatLng(47.6097, -122.3422);
var origin = new WaypointBuilder().FromLocation(start).Build();
var destination = new WaypointBuilder().FromPlaceId(place.PlaceId).Build();

var request = new RouteRequestBuilder()
    .From(origin)
    .To(destination)
    .WithTravelMode(TravelMode.Drive)
    .Build();

var result = await routes.Directions.GetRoutesAsync(request, ct: ct);
var route = result.Routes
    .FirstOrDefault(r => r.Labels.Contains(RouteLabel.DefaultRoute));

Console.WriteLine(
    $"{place.DisplayName}: {route?.DistanceMeters} m, {route?.Duration}");
```

## Next steps

- [Installation](getting-started/installation.md): requirements and packages.
- [Quick start](getting-started/quick-start.md): register a module and make your first call in an ASP.NET Core app.
- [Configuration and registration](core/configuration.md): every setting and the registration rules.
