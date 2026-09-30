namespace PriceHunt.Application.Suppliers;

/// <summary>The suppliers a search can query.</summary>
public interface ISupplierCatalog
{
    /// <summary>Gets every supplier, in display order.</summary>
    IReadOnlyList<IShippingSupplier> Suppliers { get; }
}
