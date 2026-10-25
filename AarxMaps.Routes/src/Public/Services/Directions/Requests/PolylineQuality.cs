namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Specifies how detailed the returned polylines are.
/// </summary>
public enum PolylineQuality {
    /// <summary>A high-quality polyline with more points, for detailed display.</summary>
    HighQuality,

    /// <summary>An overview polyline with fewer points, for faster rendering. Google's default.</summary>
    Overview
}
