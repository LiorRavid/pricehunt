namespace PriceHunt.Api.Searches;

/// <summary>The <c>supplier-failed</c> event.</summary>
internal sealed record SupplierFailedPayload(
    Guid SearchId,
    string SupplierId,
    string ErrorCode,
    string ErrorMessage,
    long ResponseTimeMs,
    DateTime ReceivedAt) : SearchStreamEvent(SearchId);
