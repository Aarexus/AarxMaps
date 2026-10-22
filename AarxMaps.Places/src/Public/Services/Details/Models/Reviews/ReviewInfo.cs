namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents a single user review for a place.
/// </summary>
public sealed class ReviewInfo {
    /// <summary>
    /// Gets the review text, or <c>null</c> for a rating-only review.
    /// </summary>
    public string? Text { get; internal set; }

    /// <summary>
    /// Gets the language code of the review text.
    /// </summary>
    public string? LanguageCode { get; internal set; }

    /// <summary>
    /// Gets the rating provided by the user.
    /// </summary>
    public float? Rating { get; internal set; }

    /// <summary>
    /// Gets the name of the review author.
    /// </summary>
    public string? AuthorName { get; internal set; }

    /// <summary>
    /// Gets the URI of the author's profile, if available.
    /// </summary>
    public Uri? AuthorUri { get; internal set; }

    /// <summary>
    /// Gets the publication time of the review.
    /// </summary>
    public DateTimeOffset? PublishTime { get; internal set; }
}
