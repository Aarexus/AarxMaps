namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an error caused by an invalid request sent to the Google Maps API.
/// </summary>
/// <remarks>
/// This exception is typically thrown when required parameters are missing
/// or when the request contains invalid values.
/// </remarks>
public class MapsInvalidRequestException : MapsApiException {
    private const string DefaultMessage = "The request sent to Google Maps API contains invalid or missing parameters.";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsInvalidRequestException"/> class.
    /// </summary>
    /// <param name="message">
    /// The exception message. If not provided, a default invalid request message will be used.
    /// </param>
    /// <param name="status">
    /// The status returned by the Maps API.
    /// </param>
    /// <param name="raw">
    /// The raw response returned by the Maps API.
    /// </param>
    /// <param name="inner">
    /// The inner exception that caused this exception.
    /// </param>
    public MapsInvalidRequestException(
        string? message = null,
        string? status = null,
        string? raw = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, raw, inner) { }
}