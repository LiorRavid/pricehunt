using PriceHunt.Application.Searches;
using PriceHunt.Application.Suppliers;
using PriceHunt.Application.Tests.Fakes;
using PriceHunt.Domain;

namespace PriceHunt.Application.Tests;

public sealed class SearchPlannerTests
{
    private static readonly DateOnly s_from = new(2026, 10, 1);
    private static readonly DateOnly s_to = new(2026, 10, 8);

    private readonly FakeSupplier _aurora = new("aurora");
    private readonly FakeSupplier _bluefin = new("bluefin");
    private readonly FakeSupplier _copper = new("copper");

    [Theory]
    [Trait("Requirement", "P1")]
    [InlineData(false)]
    [InlineData(true)]
    public void Uses_every_supplier_when_none_are_selected(bool emptyList)
    {
        SearchPlanResult result = Planner().Plan(Request(supplierIds: emptyList ? [] : null));

        result.IsValid.Should().BeTrue();
        result.Plan!.Suppliers.Should().Equal(_aurora, _bluefin, _copper);
    }

    [Fact]
    [Trait("Requirement", "P1")]
    public void Uses_only_the_selected_suppliers_in_catalogue_order()
    {
        SearchPlanResult result = Planner().Plan(Request(supplierIds: ["copper", "aurora"]));

        result.Plan!.Suppliers.Should().Equal(_aurora, _copper);
    }

    [Fact]
    public void Ignores_duplicate_supplier_ids()
    {
        SearchPlanResult result = Planner().Plan(Request(supplierIds: ["bluefin", "bluefin"]));

        result.Plan!.Suppliers.Should().Equal(_bluefin);
    }

    [Fact]
    [Trait("Requirement", "P1")]
    public void Rejects_unknown_supplier_ids()
    {
        SearchPlanResult result = Planner().Plan(Request(supplierIds: ["aurora", "zephyr", "acme"]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey(SearchRequestFields.SupplierIds)
            .WhoseValue.Should().ContainSingle().Which.Should().Be("Unknown supplier ids: 'acme', 'zephyr'.");
    }

    [Fact]
    public void Builds_trimmed_criteria()
    {
        SearchPlanResult result = Planner().Plan(Request(origin: "  Haifa ", destination: " Rotterdam"));

        SearchCriteria criteria = result.Plan!.Criteria;
        criteria.Route.Origin.Value.Should().Be("Haifa");
        criteria.Route.Destination.Value.Should().Be("Rotterdam");
        criteria.ShippingDates.Should().Be(ShippingDateRange.Create(s_from, s_to));
    }

    [Fact]
    public void Reports_every_missing_field_at_once()
    {
        SearchPlanResult result = Planner().Plan(new SearchRequest(null, " ", null, null, null));

        result.IsValid.Should().BeFalse();
        result.Plan.Should().BeNull();
        result.Errors.Keys.Should().BeEquivalentTo(
            [SearchRequestFields.Origin, SearchRequestFields.Destination, SearchRequestFields.FromDate, SearchRequestFields.ToDate]);
        result.Errors[SearchRequestFields.FromDate].Should().Equal("A start date is required.");
        result.Errors[SearchRequestFields.ToDate].Should().Equal("An end date is required.");
    }

    [Fact]
    public void Rejects_the_same_origin_and_destination()
    {
        SearchPlanResult result = Planner().Plan(Request(origin: "Haifa", destination: "haifa"));

        result.Errors.Should().ContainKey(SearchRequestFields.Destination)
            .WhoseValue.Should().Equal("The destination must differ from the origin.");
    }

    [Fact]
    public void Rejects_an_end_date_before_the_start_date()
    {
        SearchPlanResult result = Planner().Plan(Request(from: s_to, to: s_from));

        result.Errors.Should().ContainKey(SearchRequestFields.ToDate)
            .WhoseValue.Should().Equal("The end date can't be before the start date.");
    }

    private SearchPlanner Planner() => new(new StaticCatalog(_aurora, _bluefin, _copper));

    private static SearchRequest Request(
        string origin = "Haifa",
        string destination = "Rotterdam",
        DateOnly? from = null,
        DateOnly? to = null,
        IReadOnlyList<string>? supplierIds = null) =>
        new(origin, destination, from ?? s_from, to ?? s_to, supplierIds);

    private sealed class StaticCatalog(params IShippingSupplier[] suppliers) : ISupplierCatalog
    {
        public IReadOnlyList<IShippingSupplier> Suppliers { get; } = suppliers;
    }
}
