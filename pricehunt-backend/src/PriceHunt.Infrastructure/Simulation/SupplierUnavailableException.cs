using PriceHunt.Application.Suppliers;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>A flaky supplier's simulated outage.</summary>
internal sealed class SupplierUnavailableException(string supplierName)
    : SupplierException("supplier_unavailable", $"{supplierName} is temporarily unavailable.");
