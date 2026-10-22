namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents an AI-generated summary of a place's reviews.
/// </summary>
/// <remarks>
/// <para><b>Attribution:</b> show <see cref="DisclosureText"/> alongside the summary, and offer
/// <see cref="FlagContentUri"/> so users can report it.</para>
/// </remarks>
public sealed class ReviewSummary {
    /// <summary>
    /// Gets the summary text.
    /// </summary>
    public string Text { get; internal set; } = null!;

    /// <summary>
    /// Gets the language code of the summary.
    /// </summary>
    public string? LanguageCode { get; internal set; }

    /// <summary>
    /// Gets the AI disclosure text to display with the summary (e.g., "Summarized with Gemini").
    /// </summary>
    public string? DisclosureText { get; internal set; }

    /// <summary>
    /// Gets the URI where users can report a problem with the summary.
    /// </summary>
    public Uri? FlagContentUri { get; internal set; }
}
