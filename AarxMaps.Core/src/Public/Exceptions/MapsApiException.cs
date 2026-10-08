namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an error returned by the Google Maps API.
/// </summary>
/// <remarks>
/// This is the base exception for all errors produced by the Maps SDK.
/// It contains additional information returned by the API such as the
/// response status and the raw response payload.
/// </remarks>
public class MapsApiException : Exception {
    private const string DefaultMessage = "An error occurred while processing a request to Google Maps API.";

    /// <summary>
    /// Gets the status code returned by the Maps API, if available.
    /// </summary>
    public string? Status { get; }

    /// <summary>
    /// Gets the raw response returned by the Maps API.
    /// </summary>
    /// <remarks>
    /// This value may contain the original JSON payload returned by the API,
    /// which can be useful for debugging purposes.
    /// </remarks>
    public string? RawResponse { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsApiException"/> class.
    /// </summary>
    /// <param name="message">
    /// The exception message. If not provided, a default message will be used.
    /// </param>
    /// <param name="status">
    /// The status returned by the Maps API.
    /// </param>
    /// <param name="rawResponse">
    /// The raw response returned by the Maps API.
    /// </param>
    /// <param name="inner">
    /// The inner exception that caused this exception.
    /// </param>
    public MapsApiException(
        string? message = null,
        string? status = null,
        string? rawResponse = null,
        Exception? inner = null)
        : base(message ?? DefaultMessage, inner)
    {
        Status = status;
        RawResponse = rawResponse;
    }

    /// <summary>
    /// Returns a string representation of the exception including
    /// the API status and raw response if available.
    /// </summary>
    public override string ToString()
        => $"{GetType().Name}: {Message}\n" +
           (Status is not null ? $"Status: {Status}\n" : "") +
           (RawResponse is not null ? $"Raw: {RawResponse}\n" : "") +
           base.ToString();
}