using PriceHunt.Domain;

namespace PriceHunt.Application.Suppliers;

/// <summary>The common interface every shipping supplier implements.</summary>
public interface IShippingSupplier
{
    /// <summary>Gets the supplier's stable identifier.</summary>
    SupplierId Id { get; }

    /// <summary>Gets the name shown to users.</summary>
    string DisplayName { get; }

    /// <summary>Asks the supplier for a price.</summary>
    /// <param name="criteria">The route and shipping dates to price.</param>
    /// <param name="cancellationToken">Cancelled at the search deadline or when the client goes away.</param>
    /// <returns>The quoted price.</returns>
    /// <exception cref="SupplierException">The supplier reported a failure.</exception>
    Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken);
}
