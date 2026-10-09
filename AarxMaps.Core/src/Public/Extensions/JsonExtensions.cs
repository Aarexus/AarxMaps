using System.Text.Json;

namespace AarxMaps.Core.Extensions;

/// <summary>
/// Provides helper extension methods for safely reading values from a <see cref="JsonElement"/>.
/// </summary>
/// <remarks>
/// These methods simplify navigation through JSON structures by combining property lookup
/// and type validation. If the property does not exist or does not match the expected JSON type,
/// the methods return <c>null</c> or an empty sequence instead of throwing an exception.
///
/// This allows mappers and parsers to safely extract values from external API responses
/// without repetitive checks for property existence or <see cref="JsonValueKind"/> validation.
/// </remarks>
public static class JsonExtensions {
    /// <summary>
    /// Attempts to retrieve a JSON object property from the specified element.
    /// </summary>
    /// <param name="element">The JSON element containing the property.</param>
    /// <param name="prop">The name of the property to retrieve.</param>
    /// <returns>
    /// The <see cref="JsonElement"/> representing the object if the property exists and is of type
    /// <see cref="JsonValueKind.Object"/>; otherwise <c>null</c>.
    /// </returns>
    public static JsonElement? GetObject(this JsonElement element, string prop) 
        => element.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.Object
            ? value : null;
    
    /// <summary>
    /// Attempts to retrieve an integer property from the specified JSON element.
    /// </summary>
    /// <param name="element">The JSON element containing the property.</param>
    /// <param name="prop">The name of the property to retrieve.</param>
    /// <returns>
    /// The integer value if the property exists and is a number; otherwise <c>null</c>.
    /// </returns>
    public static int? GetInt(this JsonElement element, string prop)
        => element.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32() : null;
    
    /// <summary>
    /// Attempts to retrieve a single-precision floating-point value from the specified JSON element.
    /// </summary>
    /// <param name="element">The JSON element containing the property.</param>
    /// <param name="prop">The name of the property to retrieve.</param>
    /// <returns>
    /// The <see cref="float"/> value if the property exists and is a number; otherwise <c>null</c>.
    /// </returns>
    public static float? GetFloat(this JsonElement element, string prop)
        => element.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetSingle() : null;
    
    /// <summary>
    /// Attempts to retrieve a double-precision floating-point value from the specified JSON element.
    /// </summary>
    /// <param name="element">The JSON element containing the property.</param>
    /// <param name="prop">The name of the property to retrieve.</param>
    /// <returns>
    /// The <see cref="double"/> value if the property exists and is a number; otherwise <c>null</c>.
    /// </returns>
    public static double? GetDoubleValue(this JsonElement element, string prop)
        => element.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble() : null;
    
    /// <summary>
    /// Attempts to retrieve a boolean property from the specified JSON element.
    /// </summary>
    /// <param name="element">The JSON element containing the property.</param>
    /// <param name="prop">The name of the property to retrieve.</param>
    /// <returns>
    /// The boolean value if the property exists and is either <see cref="JsonValueKind.True"/>
    /// or <see cref="JsonValueKind.False"/>; otherwise <c>null</c>.
    /// </returns>
    public static bool? GetBool(this JsonElement element, string prop)
        => element.TryGetProperty(prop, out var value) &&
                (value.ValueKind == JsonValueKind.False || value.ValueKind == JsonValueKind.True)
            ? value.GetBoolean() : null;
    
    /// <summary>
    /// Attempts to retrieve a string property from the specified JSON element.
    /// </summary>
    /// <param name="element">The JSON element containing the property.</param>
    /// <param name="prop">The name of the property to retrieve.</param>
    /// <returns>
    /// The string value if the property exists and is of type <see cref="JsonValueKind.String"/>;
    /// otherwise <c>null</c>.
    /// </returns>
    public static string? GetStringValue(this JsonElement element, string prop)
        => element.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;
    
    /// <summary>
    /// Retrieves a JSON array enumerator for the specified property if it exists.
    /// </summary>
    /// <param name="element">The JSON element containing the property.</param>
    /// <param name="prop">The name of the array property.</param>
    /// <returns>
    /// A <see cref="JsonElement.ArrayEnumerator"/> containing the array items if the property exists
    /// and is of type <see cref="JsonValueKind.Array"/>; otherwise an empty sequence.
    /// </returns>
    public static JsonElement.ArrayEnumerator GetArray(this JsonElement element, string prop)
        => element.TryGetProperty(prop, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray() : default;
    
    /// <summary>
    /// Retrieves a JSON array property and extracts its non-null, non-empty string values.
    /// </summary>
    /// <param name="json">The JSON element containing the array property.</param>
    /// <param name="prop">The name of the array property.</param>
    /// <returns>
    /// A <see cref="IReadOnlyList{String}"/> containing all non-null and non-whitespace string values
    /// from the specified array property; otherwise, an empty list if the property is missing,
    /// not an array, or contains no valid string values.
    /// </returns>
    public static IReadOnlyList<string> GetArrayStringValues(this JsonElement json, string prop)
        => [.. json.GetArray(prop)
            .Select(s => s.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!) ];
}