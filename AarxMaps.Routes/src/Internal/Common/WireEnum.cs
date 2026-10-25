using System.Collections.Frozen;
using System.Text.Json;
using AarxMaps.Core.Exceptions;

namespace AarxMaps.Routes.Internal.Common;

// Google's enum values are the SCREAMING_SNAKE_CASE form of the public member names
// (TurnSlightLeft <-> TURN_SLIGHT_LEFT). Unrecognized values read as default(TEnum),
// which is Unknown on every enum Google returns.
internal static class WireEnum<TEnum> where TEnum : struct, Enum {
    private static readonly FrozenDictionary<TEnum, string> _toWire = Enum.GetValues<TEnum>()
        .ToFrozenDictionary(value => value, value => JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString()));
    private static readonly FrozenDictionary<string, TEnum> _fromWire = _toWire
        .ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    internal static string ToWire(TEnum value)
        => _toWire.TryGetValue(value, out var wire) ? wire
            : throw new MapsInvalidRequestException($"{value} is not a valid {typeof(TEnum).Name}.");

    internal static string? ToWire(TEnum? value)
        => value is TEnum defined ? ToWire(defined) : null;

    internal static TEnum FromWire(string? value)
        => value is not null && _fromWire.TryGetValue(value, out var parsed) ? parsed : default;
}
