namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Identifies the place to query in every <see cref="IDetailsService"/> call.
/// </summary>
/// <remarks>
/// This model is constructed using <see cref="DetailsRequestBuilder"/>.
/// </remarks>
public sealed class DetailsRequest {
    internal DetailsRequest() {}

    /// <summary>
    /// Gets the unique identifier of the place.
    /// </summary>
    public string PlaceId { get; internal set; } = null!;

    /// <summary>
    /// Gets the session token that ends an autocomplete session, or <c>null</c> when no session is used.
    /// </summary>
    public string? SessionToken { get; internal set; }
}
