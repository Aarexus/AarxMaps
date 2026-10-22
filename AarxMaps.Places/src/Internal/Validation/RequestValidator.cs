using AarxMaps.Core.Exceptions;
using AarxMaps.Places.Internal.Constants;

namespace AarxMaps.Places.Internal.Validation;

// Shared checks for consumer values used by more than one builder
internal static class RequestValidator {
    internal static string ValidateAndTrimRequiredText(string text, string name) {
        var trimmed = text?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new MapsInvalidRequestException($"Empty {name} is not allowed.");

        return trimmed;
    }

    internal static string ValidateSessionToken(string sessionToken) {
        if (string.IsNullOrEmpty(sessionToken) || sessionToken.Length > PlacesConstants.MaxSessionTokenLen)
            throw new MapsInvalidRequestException($"SessionToken must be between 1 and {PlacesConstants.MaxSessionTokenLen} characters.");
        if (!sessionToken.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            throw new MapsInvalidRequestException("SessionToken must be URL- and filename-safe base64 (A-Z, a-z, 0-9, '-', '_').");

        return sessionToken;
    }
}
