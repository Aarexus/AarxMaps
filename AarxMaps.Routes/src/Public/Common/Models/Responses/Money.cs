namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents an amount of money in a specific currency.
/// </summary>
public sealed class Money {
    /// <summary>
    /// Gets the three-letter ISO 4217 currency code (e.g., "USD", "EUR").
    /// </summary>
    public string? CurrencyCode { get; internal set; }

    /// <summary>
    /// Gets the amount, in units of <see cref="CurrencyCode"/>.
    /// </summary>
    public decimal Amount { get; internal set; }
}
