namespace AarxMaps.Core.Exceptions;

/// <summary>
/// Represents an authentication error returned by the Google Maps API.
/// </summary>
/// <remarks>
/// This exception is thrown when the provided API key is invalid,
/// missing, or does not have the required permissions to access
/// the requested service.
/// </remarks>
public class MapsApiAuthException : MapsApiException {
    private const string DefaultMessage = "The provided API Key is invalid or lacks required permissions.";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsApiAuthException"/> class.
    /// </summary>
    /// <param name="message">
    /// The exception message. If not provided, a default authentication error message will be used.
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
    public MapsApiAuthException(
        string? message = null,
        string? status = null,
        string? raw = null,
        Exception? inner = null
    ) : base(message ?? DefaultMessage, status, raw, inner) { }
}