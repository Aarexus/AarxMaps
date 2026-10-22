using System.Net;
using AarxMaps.Core.Exceptions;

namespace AarxMaps.Tests.Places.Helpers;

/// <summary>
/// Core exceptions a Places service must let through unchanged. Theories take the name
/// (serializable, one test case each) and build the exception with <see cref="Create"/>.
/// </summary>
internal static class CoreErrors {
    public static TheoryData<string> Names => new() { "auth", "invalidRequest", "notFound", "rateLimit", "api" };

    public static MapsApiException Create(string name) => name switch {
        "auth" => new MapsApiAuthException(status: HttpStatusCode.Forbidden),
        "invalidRequest" => new MapsInvalidRequestException("Google rejected the request."),
        "notFound" => new MapsNotFoundException(status: HttpStatusCode.NotFound),
        "rateLimit" => new MapsRateLimitException(status: HttpStatusCode.TooManyRequests, retryAfter: TimeSpan.FromSeconds(3)),
        "api" => new MapsApiException("Network error while calling the Maps API."),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
    };
}
