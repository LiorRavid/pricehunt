namespace PriceHunt.Application.Suppliers;

/// <summary>A failure a supplier reports on purpose, with a code and a message safe to show users.</summary>
/// <param name="errorCode">A machine-readable code, such as <c>supplier_unavailable</c>.</param>
/// <param name="message">A message safe to show users.</param>
/// <param name="innerException">The underlying cause, if any.</param>
public class SupplierException(string errorCode, string message, Exception? innerException = null)
    : Exception(RequireMessage(message), innerException)
{
    /// <summary>Gets the machine-readable error code.</summary>
    public string ErrorCode { get; } = string.IsNullOrWhiteSpace(errorCode)
        ? throw new ArgumentException("An error code is required.", nameof(errorCode))
        : errorCode;

    // Every recorded failure carries a message, so a supplier can't report one without it.
    private static string RequireMessage(string message) => string.IsNullOrWhiteSpace(message)
        ? throw new ArgumentException("A message is required.", nameof(message))
        : message;
}
