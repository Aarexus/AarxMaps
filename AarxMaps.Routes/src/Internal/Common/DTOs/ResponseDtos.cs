namespace AarxMaps.Routes.Internal.Common.DTOs;

// Response-side shapes shared by computeRoutes and computeRouteMatrix.
// Google omits zero values (0, false), so value-type fields default to them.

// One shape for the route, leg, step and matrix element localized values; each level sends a subset
internal sealed class LocalizedValuesDto {
    public LocalizedTextDto? Distance { get; set; }
    public LocalizedTextDto? Duration { get; set; }
    public LocalizedTextDto? StaticDuration { get; set; }
    public LocalizedTextDto? TransitFare { get; set; }
}

internal sealed class LocalizedTextDto {
    public string? Text { get; set; }
}

internal sealed class RouteTravelAdvisoryDto {
    public bool RouteRestrictionsPartiallyIgnored { get; set; }
    public MoneyDto? TransitFare { get; set; }
}

// google.type.Money: units is an int64 sent as a JSON string, nanos are billionths of a unit
internal sealed class MoneyDto {
    public string? CurrencyCode { get; set; }
    public long Units { get; set; }
    public int Nanos { get; set; }
}

internal sealed class FallbackInfoDto {
    public string? RoutingMode { get; set; }
    public string? Reason { get; set; }
}
