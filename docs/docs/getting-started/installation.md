---
sidebar_position: 1
title: Installation
---

# Installation

AarxMaps ships as three NuGet packages. Install the feature package you need; each one brings in `AarxMaps.Core`.

## Requirements

- .NET 8 or later. See [Supported .NET versions](#supported-net-versions).
- A Google Cloud project with billing enabled, and an API key from it. Every call the SDK makes is a billed Google request.
- The Google API behind each package enabled on that project:

| Package | Google API to enable |
| --- | --- |
| `AarxMaps.Places` | Places API (New) |
| `AarxMaps.Routes` | Routes API |

The legacy Places API isn't used. A project that only has the legacy API enabled gets authorization errors.

## Install

```bash
dotnet add package AarxMaps.Places
dotnet add package AarxMaps.Routes
```

| Package | Depends on |
| --- | --- |
| `AarxMaps.Core` | Microsoft.Extensions packages only |
| `AarxMaps.Places` | `AarxMaps.Core` |
| `AarxMaps.Routes` | `AarxMaps.Core` |

Install `AarxMaps.Core` on its own only when you want to call Google endpoints directly through `IMapsApiClient`, without a feature module. See [Custom endpoints](../core/custom-endpoints.md).

## Supported .NET versions

The packages are built for .NET 8 and .NET 9. Newer apps use the .NET 9 build, which .NET runs unchanged on later versions.

| Your app targets | Build it uses |
| --- | --- |
| .NET 8 | .NET 8 |
| .NET 9 | .NET 9 |
| .NET 10 or later | .NET 9 |

The full test suite passes on .NET 10.

## API key

- Keep the key out of source control. Use user secrets in development and environment variables or a secret store in production. [Configuration](../core/configuration.md#binding-from-configuration) shows both.
- Restrict the key in Google Cloud to the APIs you call. Google's [API security best practices](https://developers.google.com/maps/api-security-best-practices) cover the options.
- Places and Routes calls send the key in the `X-Goog-Api-Key` header, so it doesn't show up in request URLs or URL logs.

## Next steps

- [Quick start](quick-start.md): register a module and make your first call.
