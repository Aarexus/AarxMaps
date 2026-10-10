using System.Buffers;
using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text.Json;
using AarxMaps.Core.Configuration;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Json;
using AarxMaps.Core.Models;
using Microsoft.Extensions.Options;

namespace AarxMaps.Core.Internal.Caching.KeyStrategy;

internal sealed class RequestFingerprintCacheKeyStrategy : ICacheKeyStrategy {
    // -------------------- INIT --------------------
    private static readonly FrozenSet<string> AllowedHeaders =
        new [] {"Accept-Language", "Content-Type", "X-Goog-FieldMask"}
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly FrozenSet<string> ExcludedQueryParameters = 
        new [] {"key"}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    
    private static readonly JsonWriterOptions WriterOpts = new() { Indented = false };
    private readonly string KeyVersion = "v1";
    private readonly string _prefix;

    public RequestFingerprintCacheKeyStrategy(IOptions<MapsCachingOptions> opts)
        => _prefix = opts.Value.Prefix;

    // -------------------- METHS --------------------
    public string GenerateCacheKey<T>(MapsApiRequest request) {
        try {
            var buffer = new ArrayBufferWriter<byte>(512);

            using (var writer = new Utf8JsonWriter(buffer, WriterOpts)) {
                writer.WriteStartObject();
                writer.WriteString("type", typeof(T).FullName ?? typeof(T).Name);
                writer.WriteString("base", request.BaseUrl.AbsoluteUri);
                writer.WriteString("endpoint", request.Endpoint.Trim());
                writer.WriteString("method", request.Method.Method.ToUpperInvariant());
                WriteMap(writer, "headers", request.Headers,
                    include: k => AllowedHeaders.Contains(k), ignoreCase: true);
                WriteMap(writer, "query", request.Query,
                    include: k => !ExcludedQueryParameters.Contains(k), ignoreCase: false);
                WritePayload(writer, request.Payload);
                writer.WriteEndObject();
            }

            var hash = SHA256.HashData(buffer.WrittenSpan);
            return $"{_prefix}:{KeyVersion}:{Convert.ToHexString(hash)}";
        }
        catch (Exception ex) when (ex is not MapsCacheException and not OperationCanceledException) {
            throw new MapsCacheException("Cache key generation failed.", inner: ex);
        }
    }

    // -------------------- INNER METHS --------------------
     private static void WriteMap(
        Utf8JsonWriter w, string name, IDictionary<string, string>? dict,
        Func<string, bool> include, bool ignoreCase
    ) {
        w.WritePropertyName(name);
        w.WriteStartObject();

        if (dict is not null) {
            var entries = dict
                .Where(kv => include(kv.Key))
                .Select(kv => (
                    Key: ignoreCase ? kv.Key.Trim().ToLowerInvariant() : kv.Key.Trim(),
                    Value: kv.Value?.Trim() ?? string.Empty))
                .OrderBy(e => e.Key, StringComparer.Ordinal);

            foreach (var (key, value) in entries)
                w.WriteString(key, value);
        }

        w.WriteEndObject();
    }

    private static void WritePayload(Utf8JsonWriter w, object? payload) {
        w.WritePropertyName("payload");

        if (payload is null) {
            w.WriteNullValue();
            return;
        }

        using var doc = JsonSerializer.SerializeToDocument(payload, payload.GetType(), MapsJsonOptions.Default);
        WriteCanonical(w, doc.RootElement);
    }

    private static void WriteCanonical(Utf8JsonWriter w, JsonElement element) {
        switch (element.ValueKind) {
            case JsonValueKind.Object:
                w.WriteStartObject();
                foreach (var p in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)) {
                    w.WritePropertyName(p.Name);
                    WriteCanonical(w, p.Value);
                }
                w.WriteEndObject();
                break;

            case JsonValueKind.Array:
                w.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteCanonical(w, item);
                w.WriteEndArray();
                break;

            case JsonValueKind.String: w.WriteStringValue(element.GetString()); break;
            case JsonValueKind.Number: w.WriteRawValue(element.GetRawText()); break;
            case JsonValueKind.True:   w.WriteBooleanValue(true); break;
            case JsonValueKind.False:  w.WriteBooleanValue(false); break;
            case JsonValueKind.Null:   w.WriteNullValue(); break;

            default:
                throw new MapsCacheException("Unsupported JSON token during payload normalization.");
        }
    }
}