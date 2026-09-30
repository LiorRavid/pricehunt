using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PriceHunt.Application.Suppliers;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Infrastructure.Tests.Simulation;

public sealed class SimulationConfigurationTests
{
    [Fact]
    [Trait("Requirement", "S1")]
    [Trait("Requirement", "S2")]
    [Trait("Requirement", "S3")]
    [Trait("Requirement", "S4")]
    public void The_shipped_configuration_defines_seven_suppliers_one_flaky_and_one_silent()
    {
        SimulationOptions options = LoadShippedOptions();

        new SimulationOptionsValidator().Validate(null, options).Succeeded.Should().BeTrue();
        options.Suppliers.Should().HaveCount(7);
        options.Suppliers.Select(supplier => supplier.Name).Should().OnlyHaveUniqueItems();
        options.MinDelay.Should().Be(TimeSpan.FromMilliseconds(500));
        options.MaxDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.Seed.Should().BeNull("demos are random unless Simulation__Seed is set");
        options.Suppliers.Should().ContainSingle(supplier => supplier.Behavior == SupplierBehavior.Flaky)
            .Which.FailureRate.Should().Be(0.3);
        options.Suppliers.Should().ContainSingle(supplier => supplier.Behavior == SupplierBehavior.Unresponsive);
    }

    [Fact]
    [Trait("Requirement", "S1")]
    public void The_catalogue_builds_one_supplier_per_entry_in_configuration_order()
    {
        SimulationOptions options = LoadShippedOptions();

        var catalog = new SimulatedSupplierCatalog(Options.Create(options), new FakeTimeProvider());

        catalog.Suppliers.Select(supplier => supplier.Id.Value).Should().Equal(options.Suppliers.Select(supplier => supplier.Id));
        catalog.Suppliers.Should().ContainSingle(supplier => supplier is FlakySupplier);
        catalog.Suppliers.Should().ContainSingle(supplier => supplier is UnresponsiveSupplier);
        catalog.Suppliers.OfType<ReliableSupplier>().Should().HaveCount(5);
        catalog.Suppliers.Should().AllBeAssignableTo<IShippingSupplier>();
    }

    [Fact]
    public void The_same_seed_draws_the_same_sequence()
    {
        var first = new SeededRandomSource(1234);
        var second = new SeededRandomSource(1234);

        long[] firstDraws = [.. Enumerable.Range(0, 20).Select(_ => first.NextInt64(500, 5_001))];
        long[] secondDraws = [.. Enumerable.Range(0, 20).Select(_ => second.NextInt64(500, 5_001))];

        firstDraws.Should().Equal(secondDraws);
    }

    [Theory]
    [InlineData("no suppliers", "must list at least one supplier")]
    [InlineData("inverted delays", "MinDelay")]
    [InlineData("duplicate id", "configured more than once")]
    [InlineData("invalid id", "lowercase slug")]
    [InlineData("blank name", "needs a name")]
    [InlineData("inverted prices", "price range")]
    [InlineData("impossible failure rate", "FailureRate")]
    public void Rejects_invalid_simulation_settings(string scenario, string expectedFailure)
    {
        ValidateOptionsResult result = new SimulationOptionsValidator().Validate(null, InvalidOptions(scenario));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(expectedFailure);
    }

    private static SimulationOptions InvalidOptions(string scenario)
    {
        var supplier = new SimulatedSupplierOptions { Id = "albatross-freight", Name = "Albatross Freight", MinPrice = 100m, MaxPrice = 200m };
        var options = new SimulationOptions { Suppliers = [supplier] };
        switch (scenario)
        {
            case "no suppliers":
                options.Suppliers.Clear();
                break;
            case "inverted delays":
                options.MinDelay = TimeSpan.FromSeconds(10);
                break;
            case "duplicate id":
                options.Suppliers.Add(supplier with { });
                break;
            case "invalid id":
                supplier.Id = "Not A Slug";
                break;
            case "blank name":
                supplier.Name = " ";
                break;
            case "inverted prices":
                supplier.MaxPrice = 1m;
                break;
            default:
                supplier.Behavior = SupplierBehavior.Flaky;
                supplier.FailureRate = 1.5;
                break;
        }

        return options;
    }

    private static SimulationOptions LoadShippedOptions()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "shipped-appsettings.json"), optional: false)
            .Build();
        return configuration.GetSection(SimulationOptions.SectionName).Get<SimulationOptions>()
            ?? throw new InvalidOperationException("The shipped configuration has no Simulation section.");
    }
}
