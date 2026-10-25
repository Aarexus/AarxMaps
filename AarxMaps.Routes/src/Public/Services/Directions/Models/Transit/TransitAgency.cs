namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents an agency that operates a transit line.
/// </summary>
public sealed class TransitAgency {
    /// <summary>
    /// Gets the name of the agency.
    /// </summary>
    public string? Name { get; internal set; }

    /// <summary>
    /// Gets the agency's website, or <c>null</c> when Google returns none or the link is not http or https.
    /// </summary>
    public Uri? Uri { get; internal set; }
}
