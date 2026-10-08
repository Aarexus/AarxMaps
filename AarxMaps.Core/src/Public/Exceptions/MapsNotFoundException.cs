namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an error indicating that the requested resource
/// could not be found in the Google Maps API.
/// </summary>
public class MapsNotFoundException : MapsApiException {
    private const string DefaultMessage = "The requested resource was not found in Google Maps API.";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsNotFoundException"/> class.
    /// </summary>
    /// <param name="message">
    /// The exception message. If not provided, a default not-found message will be used.
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
    public MapsNotFoundException(
        string? message = null,
        string? status = null,
        string? raw = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, raw, inner) { }
}