using System.Collections.Frozen;
using AarxMaps.Core.Models;
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Places.Internal.Constants;

namespace AarxMaps.Places.Internal.Services.Autocomplete;

internal static class AutocompleteFactory {
    private static readonly FrozenDictionary<string, string> _defHeaders = new Dictionary<string, string>() {
        {"X-Goog-FieldMask", "suggestions.placePrediction.placeId,suggestions.placePrediction.text,suggestions.placePrediction.types"}
    }.ToFrozenDictionary();

    internal static MapsApiRequest CreateRequest(AutocompleteRequest request)
        => new () {
            Method = HttpMethod.Post,
            BaseUrl = PlacesEndpoints.BaseUrl,
            Endpoint = PlacesEndpoints.Autocomplete,
            Headers = _defHeaders,
            Payload = request
        };
}
