using System.Collections.Frozen;
using System.Globalization;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models;
using AarxMaps.Places.Internal.Constants;
using AarxMaps.Places.Services.Details;

namespace AarxMaps.Places.Internal.Services.Details;

internal static class DetailsFactory {
    // -------------------- INIT --------------------
    private static readonly FrozenDictionary<string, string> _detailsHeaders = CreateHeaders(
        "displayName", "types", // General Info
        "formattedAddress", "plusCode", "location", "postalAddress", "timeZone", // Location-related Info
        "nationalPhoneNumber", "internationalPhoneNumber", // Communication Info
        "priceLevel", "rating", "userRatingCount", // Rating Info
        "googleMapsUri", "websiteUri", "googleMapsLinks", // Links Info
        "paymentOptions", "parkingOptions", // Options Info
        "regularOpeningHours" // Business Info
    );
    private static readonly FrozenDictionary<string, string> _reviewsHeaders = CreateHeaders(
        "displayName", "reviewSummary", "reviews", "googleMapsLinks"
    );
    private static readonly FrozenDictionary<string, string> _photosHeaders = CreateHeaders("photos");

    // -------------------- METHS --------------------
    internal static MapsApiRequest CreateDetailsRequest(DetailsRequest request)
        => CreatePlaceRequest(request, _detailsHeaders, cacheable: true);

    internal static MapsApiRequest CreateReviewsRequest(DetailsRequest request)
        => CreatePlaceRequest(request, _reviewsHeaders, cacheable: true);

    // Photo names expire and Google doesn't allow caching them
    internal static MapsApiRequest CreatePhotosRequest(DetailsRequest request)
        => CreatePlaceRequest(request, _photosHeaders, cacheable: false);

    internal static MapsApiRequest CreateRawDetailsRequest(DetailsRequest request, IEnumerable<string> fields) {
        var mask = NormalizeFields(fields);
        var headers = new Dictionary<string, string> { { "X-Goog-FieldMask", string.Join(',', mask) } };

        return CreatePlaceRequest(request, headers, cacheable: !mask.Exists(IncludesPhotos));
    }

    internal static MapsApiRequest CreatePhotoMediaRequest(string photoName, PhotoOptions options)
        => new () {
            Method = HttpMethod.Get,
            BaseUrl = PlacesEndpoints.BaseUrl,
            Endpoint = $"{photoName}/media",
            Query = new Dictionary<string, string> {
                { "skipHttpRedirect", "true" },
                { "maxHeightPx", options.MaxHeightPx.ToString(CultureInfo.InvariantCulture) },
                { "maxWidthPx", options.MaxWidthPx.ToString(CultureInfo.InvariantCulture) }
            },
            Cacheable = false
        };

    // -------------------- INNER METHS --------------------
    private static MapsApiRequest CreatePlaceRequest(
        DetailsRequest request, IDictionary<string, string> headers, bool cacheable
    ) => new () {
            Method = HttpMethod.Get,
            BaseUrl = PlacesEndpoints.BaseUrl,
            Endpoint = $"{PlacesEndpoints.Details}/{Uri.EscapeDataString(request.PlaceId)}",
            Headers = headers,
            Query = request.SessionToken is null ? null
                : new Dictionary<string, string> { { "sessionToken", request.SessionToken } },
            Cacheable = cacheable
        };

    private static FrozenDictionary<string, string> CreateHeaders(params string[] fields)
        => new Dictionary<string, string> { { "X-Goog-FieldMask", string.Join(',', fields) } }
            .ToFrozenDictionary();

    private static List<string> NormalizeFields(IEnumerable<string> fields) {
        if (fields is null)
            throw new MapsInvalidRequestException("Fields cannot be null.");

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields) {
            var trimmed = field?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new MapsInvalidRequestException("Empty fields are not allowed.");
            if (seen.Add(trimmed))
                normalized.Add(trimmed);
        }

        if (normalized.Count == 0)
            throw new MapsInvalidRequestException("At least one field is required.");

        return normalized;
    }

    private static bool IncludesPhotos(string field)
        => field is "*" or "photos" || field.StartsWith("photos.", StringComparison.Ordinal);
}
