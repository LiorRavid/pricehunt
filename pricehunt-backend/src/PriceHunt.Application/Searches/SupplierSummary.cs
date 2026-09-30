using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>A supplier as the client sees it.</summary>
/// <param name="Id">The supplier id.</param>
/// <param name="Name">The display name.</param>
public sealed record SupplierSummary(SupplierId Id, string Name);
