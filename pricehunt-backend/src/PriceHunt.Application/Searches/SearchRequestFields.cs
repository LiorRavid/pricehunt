namespace PriceHunt.Application.Searches;

/// <summary>The field names validation errors are reported under, matching the request's JSON properties.</summary>
public static class SearchRequestFields
{
    /// <summary>The origin field.</summary>
    public const string Origin = "origin";

    /// <summary>The destination field.</summary>
    public const string Destination = "destination";

    /// <summary>The first-shipping-day field.</summary>
    public const string FromDate = "fromDate";

    /// <summary>The last-shipping-day field.</summary>
    public const string ToDate = "toDate";

    /// <summary>The supplier-selection field.</summary>
    public const string SupplierIds = "supplierIds";
}
