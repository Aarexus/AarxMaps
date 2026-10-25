namespace AarxMaps.Routes.Internal.Constants;

internal static class RoutesEndpoints {
    public static readonly Uri BaseUrl = new ("https://routes.googleapis.com/");
    public const string ComputeRoutes = "directions/v2:computeRoutes";
}
