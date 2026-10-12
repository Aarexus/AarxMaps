using System.Text.Json;
using System.Text.Json.Serialization;

namespace AarxMaps.Core.Internal.Json;

internal static class MapsJsonOptions {
    public static readonly JsonSerializerOptions Default = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        
        PropertyNameCaseInsensitive = true,

        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}