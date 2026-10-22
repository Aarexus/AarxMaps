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
    
    internal static string[] ValidateAndTrimList(IEnumerable<string> values, string name, int max) {
        var valuesColl = values?.ToArray()
            ?? throw new MapsInvalidRequestException($"{name} cannot be null.");

        var len = valuesColl.Length;
        if (len == 0 || len > max)
            throw new MapsInvalidRequestException($"Between 1 and {max} {name} can be specified.");

        for (var i = 0; i < len; i++) {
            var type = valuesColl[i]?.Trim();
            if (string.IsNullOrWhiteSpace(type))
                throw new MapsInvalidRequestException($"Empty {name} values are not allowed.");
            valuesColl[i] = type;
        }

        return valuesColl;
    }

    internal static string ValidateSessionToken(string sessionToken) {
        if (string.IsNullOrEmpty(sessionToken) || sessionToken.Length > PlacesConstants.MaxSessionTokenLen)
            throw new MapsInvalidRequestException($"SessionToken must be between 1 and {PlacesConstants.MaxSessionTokenLen} characters.");
        if (!sessionToken.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
            throw new MapsInvalidRequestException("SessionToken must be URL- and filename-safe base64 (A-Z, a-z, 0-9, '-', '_').");

        return sessionToken;
    }
}
