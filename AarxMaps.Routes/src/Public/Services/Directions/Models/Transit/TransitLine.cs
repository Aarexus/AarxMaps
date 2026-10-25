namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents a transit line, such as a bus or subway line.
/// </summary>
public sealed class TransitLine {
    /// <summary>
    /// Gets the full name of the line (e.g., "Sunset Boulevard Line").
    /// </summary>
    public string? Name { get; internal set; }

    /// <summary>
    /// Gets the short name of the line, usually a number or a code (e.g., "M7").
    /// </summary>
    public string? ShortName { get; internal set; }

    /// <summary>
    /// Gets the color commonly used for the line, as a hex string (e.g., "#FF0000").
    /// </summary>
    public string? Color { get; internal set; }

    /// <summary>
    /// Gets the color for text drawn on <see cref="Color"/>, as a hex string.
    /// </summary>
    public string? TextColor { get; internal set; }

    /// <summary>
    /// Gets the kind of vehicle that serves the line.
    /// </summary>
    public TransitVehicleType VehicleType { get; internal set; }

    /// <summary>
    /// Gets the name of the vehicle in the request language (e.g., "Subway").
    /// </summary>
    public string? VehicleName { get; internal set; }

    /// <summary>
    /// Gets the agencies that operate the line.
    /// </summary>
    /// <remarks>
    /// Show the agency names and links wherever transit directions are displayed.
    /// </remarks>
    public IReadOnlyList<TransitAgency> Agencies { get; internal set; } = [];
}
