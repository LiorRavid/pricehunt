using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>How one supplier call ended. A call never throws; every ending is one of these.</summary>
internal abstract record SupplierCallResult(IShippingSupplier Supplier, TimeSpan Elapsed, DateTime EndedAt);

/// <summary>The supplier returned a price.</summary>
internal sealed record QuotedCall(IShippingSupplier Supplier, Money Price, TimeSpan Elapsed, DateTime EndedAt)
    : SupplierCallResult(Supplier, Elapsed, EndedAt);

/// <summary>The supplier failed.</summary>
internal sealed record FailedCall(IShippingSupplier Supplier, string ErrorCode, string ErrorMessage, TimeSpan Elapsed, DateTime EndedAt)
    : SupplierCallResult(Supplier, Elapsed, EndedAt);

/// <summary>The call was cancelled by the deadline or the caller; it is closed when the search ends.</summary>
internal sealed record InterruptedCall(IShippingSupplier Supplier, TimeSpan Elapsed, DateTime EndedAt)
    : SupplierCallResult(Supplier, Elapsed, EndedAt);
