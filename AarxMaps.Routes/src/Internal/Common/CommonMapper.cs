using System.Globalization;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Common.DTOs;

namespace AarxMaps.Routes.Internal.Common;

// Conversions shared by Directions and DistanceMatrix, in both directions
internal static class CommonMapper {
    // -------------------- INIT --------------------
    private const decimal NanosPerUnit = 1_000_000_000m;

    // -------------------- REQUEST --------------------
    internal static WaypointDto ToWaypointDto(Waypoint waypoint)
        => new () {
            Location = waypoint.Location is null ? null : ToLocationDto(waypoint.Location),
            PlaceId = waypoint.PlaceId,
            Address = waypoint.Address,
            Via = waypoint.Via,
            VehicleStopover = waypoint.VehicleStopover,
            SideOfRoad = waypoint.SideOfRoad
        };

    internal static RouteModifiersDto? ToRouteModifiersDto(RouteModifiers? modifiers)
        => modifiers is null ? null
            : new RouteModifiersDto() {
                AvoidTolls = modifiers.AvoidTolls,
                AvoidHighways = modifiers.AvoidHighways,
                AvoidFerries = modifiers.AvoidFerries,
                AvoidIndoor = modifiers.AvoidIndoor,
                VehicleInfo = modifiers.EmissionType is VehicleEmissionType emissionType
                    ? new VehicleInfoDto() { EmissionType = WireEnum<VehicleEmissionType>.ToWire(emissionType) }
                    : null
            };

    internal static TransitPreferencesDto? ToTransitPreferencesDto(TransitPreferences? preferences)
        => preferences is null ? null
            : new TransitPreferencesDto() {
                AllowedTravelModes = preferences.AllowedTravelModes.Count == 0 ? null
                    : [.. preferences.AllowedTravelModes.Select(mode => WireEnum<TransitTravelMode>.ToWire(mode))],
                RoutingPreference = WireEnum<TransitRoutingPreference>.ToWire(preferences.RoutingPreference)
            };

    // RFC 3339 in UTC with whole seconds; Google only accepts 0, 3, 6 or 9 fractional digits
    // and DateTimeOffset's default format writes 7
    internal static string? ToTimestamp(DateTimeOffset? value)
        => value?.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    // -------------------- RESPONSE --------------------
    // google.protobuf.Duration: seconds with an "s" suffix and up to 9 fractional digits ("1234s", "1.5s")
    internal static TimeSpan ToDuration(string? duration) {
        if (duration is null || !duration.EndsWith('s')) return TimeSpan.Zero;

        return decimal.TryParse(
            duration.AsSpan(0, duration.Length - 1), NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var seconds
        ) ? TimeSpan.FromTicks((long)(seconds * TimeSpan.TicksPerSecond)) : TimeSpan.Zero;
    }

    internal static DateTimeOffset? ToTimestamp(string? timestamp)
        => DateTimeOffset.TryParse(
            timestamp, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed
        ) ? parsed : null;

    internal static LatLng? ToLatLng(LatLngDto? dto)
        => dto is null ? null : new LatLng(dto.Latitude, dto.Longitude);

    internal static Location? ToLocation(LocationDto? dto)
        => dto?.LatLng is null ? null
            : new Location() { LatLng = new LatLng(dto.LatLng.Latitude, dto.LatLng.Longitude), Heading = dto.Heading };

    internal static RouteTravelAdvisory? ToTravelAdvisory(RouteTravelAdvisoryDto? dto)
        => dto is null ? null
            : new RouteTravelAdvisory() {
                RouteRestrictionsPartiallyIgnored = dto.RouteRestrictionsPartiallyIgnored,
                TransitFare = ToMoney(dto.TransitFare)
            };

    internal static RouteLocalizedValues? ToLocalizedValues(LocalizedValuesDto? dto)
        => dto is null ? null
            : new RouteLocalizedValues() {
                Distance = dto.Distance?.Text,
                Duration = dto.Duration?.Text,
                StaticDuration = dto.StaticDuration?.Text,
                TransitFare = dto.TransitFare?.Text
            };

    internal static FallbackInfo? ToFallbackInfo(FallbackInfoDto? dto)
        => dto is null ? null
            : new FallbackInfo() {
                RoutingMode = WireEnum<FallbackRoutingMode>.FromWire(dto.RoutingMode),
                Reason = WireEnum<FallbackReason>.FromWire(dto.Reason)
            };

    // Only web links: anything else (javascript:, file:, Unix paths read as file:///) is dropped
    internal static Uri? ToUri(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri : null;

    // -------------------- INNER METHS --------------------
    private static LocationDto ToLocationDto(Location location)
        => new () {
            LatLng = new LatLngDto() { Latitude = location.LatLng.Latitude, Longitude = location.LatLng.Longitude },
            Heading = location.Heading
        };

    private static Money? ToMoney(MoneyDto? dto)
        => dto is null ? null
            : new Money() { CurrencyCode = dto.CurrencyCode, Amount = dto.Units + dto.Nanos / NanosPerUnit };
}
