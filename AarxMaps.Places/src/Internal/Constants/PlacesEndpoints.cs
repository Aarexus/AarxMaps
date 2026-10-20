namespace AarxMaps.Places.Internal.Constants;

internal static class PlacesEndpoints {
    public static Uri BaseUrl => new ("https://places.googleapis.com/v1/");
    public const string Autocomplete = "places:autocomplete";
}