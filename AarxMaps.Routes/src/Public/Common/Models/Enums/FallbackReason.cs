namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents why Google fell back from the requested routing mode.
/// </summary>
public enum FallbackReason {
    /// <summary>A value Google returned that this SDK does not recognize.</summary>
    Unknown,

    /// <summary>A server error happened while computing with the requested mode.</summary>
    ServerError,

    /// <summary>The requested mode could not finish in time.</summary>
    LatencyExceeded
}
