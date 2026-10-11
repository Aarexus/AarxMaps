using System.Net;

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
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets the raw response returned by the Maps API.
    /// </summary>
    /// <remarks>
    /// This value may contain the original JSON payload returned by the API,
    /// which can be useful for debugging purposes.
    /// </remarks>
    public string? ResponseBody { get; }

    public MapsApiException(
        string? message = null,
        HttpStatusCode? status = null,
        string? response = null,
        Exception? inner = null)
        : base(message ?? DefaultMessage, inner)
    {
        StatusCode = status;
        ResponseBody = response;
    }

    /// <summary>
    /// Returns a string representation of the exception including
    /// the API status and raw response if available.
    /// </summary>
    public override string ToString()
        => $"{GetType().Name}: {Message}\n" +
           (StatusCode is not null ? $"StatusCode: {StatusCode}\n" : "") +
           (ResponseBody is not null ? $"ResponseBody: {ResponseBody}\n" : "") +
           base.ToString();
}