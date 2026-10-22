using System.Runtime.CompilerServices;
using System.Text.Json;
using AarxMaps.Core.Exceptions;

namespace AarxMaps.Core.Internal.Json;

internal static class MapsJsonSerializer {
    // -------------------- METHS --------------------
    public static string Serialize(object value) 
        => JsonSerializer.Serialize(value, MapsJsonOptions.Default);

    public static T? Deserialize<T>(string? json) {
        if (string.IsNullOrWhiteSpace(json)) return default;

        try {
            return JsonSerializer.Deserialize<T>(json, MapsJsonOptions.Default);
        }
        catch (JsonException ex) {
            throw new MapsApiException($"Response could not be deserialized to {typeof(T).Name}.", inner: ex);
        }
    }

    /// <summary>
    /// Incrementally deserializes a top-level JSON array from a stream. The stream is not
    /// disposed; the caller owns it. <c>null</c> elements are skipped.
    /// </summary>
    /// <remarks>
    /// When <typeparamref name="T"/> is <see cref="JsonDocument"/>, the caller must dispose each item.
    /// </remarks>
    /// <exception cref="MapsApiException">The JSON is malformed or does not match <typeparamref name="T"/>.</exception>
    public static async IAsyncEnumerable<T> DeserializeStreamAsync<T>(
        Stream utf8Json, [EnumeratorCancellation] CancellationToken ct
    ) {
        ArgumentNullException.ThrowIfNull(utf8Json);

        var enumerator = JsonSerializer
            .DeserializeAsyncEnumerable<T>(utf8Json, MapsJsonOptions.Default, ct)
            .GetAsyncEnumerator(ct);
        await using var enumeratorLifetime = enumerator.ConfigureAwait(false);

        while (true) {
            T? item;
            try {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) yield break;
                item = enumerator.Current;
            }
            catch (JsonException ex) {
                throw new MapsApiException($"Stream could not be deserialized to {typeof(T).Name}.", inner: ex);
            }

            if (item is not null)
                yield return item;
        }
    }
}