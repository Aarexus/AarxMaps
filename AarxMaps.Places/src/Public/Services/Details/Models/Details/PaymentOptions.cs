namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Represents payment options supported by a place.
/// </summary>
public sealed class PaymentOptions {
    /// <summary>
    /// Gets whether credit cards are accepted.
    /// </summary>
    public bool? AcceptsCreditCards { get; internal set; }

    /// <summary>
    /// Gets whether debit cards are accepted.
    /// </summary>
    public bool? AcceptsDebitCards { get; internal set; }

    /// <summary>
    /// Gets whether only cash is accepted.
    /// </summary>
    public bool? AcceptsCashOnly { get; internal set; }
    
    /// <summary>
    /// Gets whether NFC payments are accepted.
    /// </summary>
    public bool? AcceptsNfc { get; internal set; }
}