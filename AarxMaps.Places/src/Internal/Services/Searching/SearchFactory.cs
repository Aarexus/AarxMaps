using System.Collections.Frozen;
using AarxMaps.Core.Models;
using AarxMaps.Places.Internal.Constants;
using AarxMaps.Places.Services.Searching;

namespace AarxMaps.Places.Internal.Services.Searching;

internal static class SearchFactory {
    private static readonly FrozenDictionary<string, string> _txtDefHeaders = new Dictionary<string, string>() {
        {"X-Goog-FieldMask", "nextPageToken,places.id,places.displayName,places.types"}
    }.ToFrozenDictionary();
    private static readonly FrozenDictionary<string, string> _nearbyDefHeaders = new Dictionary<string, string>() {
        {"X-Goog-FieldMask", "places.id,places.displayName,places.types"}
    }.ToFrozenDictionary();

    internal static MapsApiRequest CreateTextRequest(TextRequest request)
        => new () {
            Method = HttpMethod.Post,
            BaseUrl = PlacesEndpoints.BaseUrl,
            Endpoint = PlacesEndpoints.SearchText,
            Headers = _txtDefHeaders,
            Payload = request
        };
    internal static MapsApiRequest CreateNearbyRequest(NearbyRequest request)
        => new () {
            Method = HttpMethod.Post,
            BaseUrl = PlacesEndpoints.BaseUrl,
            Endpoint = PlacesEndpoints.SearchNearby,
            Headers = _nearbyDefHeaders,
            Payload = request
        };
}
