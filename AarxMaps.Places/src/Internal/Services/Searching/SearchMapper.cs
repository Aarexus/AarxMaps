using AarxMaps.Places.Internal.Services.Searching.DTOs;
using AarxMaps.Places.Services.Searching;

namespace AarxMaps.Places.Internal.Services.Searching;

internal static class SearchMapper {
    // -------------------- METHS --------------------
    internal static TextSearchResult ToTextResult(SearchResponse response) {
        var pageToken = response.NextPageToken;
        return new () { 
            Places = MapPlacesInfo(response.Places), 
            NextPageToken = string.IsNullOrEmpty(pageToken) ? null : pageToken
        };
    }
    internal static NearbySearchResult ToNearbyResult(SearchResponse response)
        => new () { Places = MapPlacesInfo(response.Places) };

    // -------------------- INNER METHS --------------------
    private static List<PlaceSearchInfo> MapPlacesInfo(List<PlaceDto> placesResponse) {
        var places = new List<PlaceSearchInfo>(placesResponse.Count);

        foreach (var place in placesResponse) {
            if (string.IsNullOrWhiteSpace(place.Id) || string.IsNullOrWhiteSpace(place.DisplayName?.Text))
                continue;

            places.Add(new PlaceSearchInfo() {
                PlaceId = place.Id,
                DisplayName = place.DisplayName.Text,
                Types = [.. place.Types]
            });
        }

        return places;
    }
}
