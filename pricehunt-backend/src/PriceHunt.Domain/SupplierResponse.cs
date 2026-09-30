namespace PriceHunt.Domain;

/// <summary>
/// How one selected supplier's part in a search ended. Only a success carries a price and only a
/// failure carries an error; a supplier that never answered is recorded as timed out or cancelled.
/// </summary>
public sealed class SupplierResponse
{
    private SupplierResponse(
        Guid id,
        SupplierId supplierId,
        ResponseOutcome outcome,
        Money? price,
        TimeSpan responseTime,
        DateTime receivedAt,
        string? errorCode,
        string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(supplierId);
        ArgumentOutOfRangeException.ThrowIfLessThan(responseTime, TimeSpan.Zero);

        Id = id;
        SupplierId = supplierId;
        Outcome = outcome;
        Price = price;
        ResponseTime = responseTime;
        ReceivedAt = UtcTimestamp.Require(receivedAt, nameof(receivedAt));
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets the response's time-ordered identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the supplier this response belongs to.</summary>
    public SupplierId SupplierId { get; }

    /// <summary>Gets how the supplier's part ended.</summary>
    public ResponseOutcome Outcome { get; }

    /// <summary>Gets the quoted price; only a success has one.</summary>
    public Money? Price { get; }

    /// <summary>Gets how long the supplier took, or how long it was waited for.</summary>
    public TimeSpan ResponseTime { get; }

    /// <summary>Gets when the outcome was recorded (UTC).</summary>
    public DateTime ReceivedAt { get; }

    /// <summary>Gets the machine-readable error; only a failure has one.</summary>
    public string? ErrorCode { get; }

    /// <summary>Gets the human-readable error; only a failure has one.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Restores a persisted response, checking that its fields fit its outcome.</summary>
    /// <param name="id">The response id.</param>
    /// <param name="supplierId">The supplier.</param>
    /// <param name="outcome">How the supplier's part ended.</param>
    /// <param name="price">The price, for a success only.</param>
    /// <param name="responseTime">How long the supplier took or was waited for.</param>
    /// <param name="receivedAt">When the outcome was recorded (UTC).</param>
    /// <param name="errorCode">The error code, for a failure only.</param>
    /// <param name="errorMessage">The error message, for a failure only.</param>
    /// <returns>The response.</returns>
    /// <exception cref="ArgumentException">The fields don't fit the outcome.</exception>
    public static SupplierResponse Restore(
        Guid id,
        SupplierId supplierId,
        ResponseOutcome outcome,
        Money? price,
        TimeSpan responseTime,
        DateTime receivedAt,
        string? errorCode,
        string? errorMessage)
    {
        bool hasPrice = price is not null;
        bool hasError = !string.IsNullOrEmpty(errorCode);
        bool consistent = outcome switch
        {
            ResponseOutcome.Succeeded => hasPrice && !hasError,
            ResponseOutcome.Failed => !hasPrice && hasError,
            _ => !hasPrice && !hasError,
        };

        return consistent
            ? new SupplierResponse(id, supplierId, outcome, price, responseTime, receivedAt, errorCode, errorMessage)
            : throw new ArgumentException($"A {outcome} response can't have these price and error fields.", nameof(outcome));
    }

    internal static SupplierResponse Succeeded(SupplierId supplierId, Money price, TimeSpan responseTime, DateTime receivedAt)
    {
        ArgumentNullException.ThrowIfNull(price);
        return new SupplierResponse(NewId(receivedAt), supplierId, ResponseOutcome.Succeeded, price, responseTime, receivedAt, null, null);
    }

    internal static SupplierResponse Failed(
        SupplierId supplierId,
        string errorCode,
        string errorMessage,
        TimeSpan responseTime,
        DateTime receivedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new SupplierResponse(NewId(receivedAt), supplierId, ResponseOutcome.Failed, null, responseTime, receivedAt, errorCode, errorMessage);
    }

    internal static SupplierResponse NoResponse(SupplierId supplierId, ResponseOutcome outcome, TimeSpan waited, DateTime at) =>
        new(NewId(at), supplierId, outcome, null, waited, at, null, null);

    private static Guid NewId(DateTime timestamp) =>
        Guid.CreateVersion7(new DateTimeOffset(UtcTimestamp.Require(timestamp, nameof(timestamp))));
}
