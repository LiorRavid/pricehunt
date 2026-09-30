using System.Net.Http.Json;

namespace PriceHunt.Api.Tests;

public sealed class SupplierEndpointTests(PriceHuntApiFactory factory) : IClassFixture<PriceHuntApiFactory>
{
    [Fact]
    [Trait("Requirement", "S1")]
    public async Task Lists_the_configured_suppliers_in_display_order()
    {
        using HttpClient client = factory.CreateClient();

        SupplierItem[]? suppliers = await client.GetFromJsonAsync<SupplierItem[]>("/api/suppliers", TestContext.Current.CancellationToken);

        suppliers.Should().NotBeNull();
        suppliers!.Select(supplier => supplier.Name).Should().Equal(
            "Albatross Freight",
            "Bramblewood Cargo",
            "Cobalt Harbor Lines",
            "Driftwood Shipping",
            "Emberline Logistics",
            "Foxglove Freightways",
            "Gullwing Transport");
        suppliers[0].Id.Should().Be("albatross-freight");
    }

    private sealed record SupplierItem(string Id, string Name);
}
