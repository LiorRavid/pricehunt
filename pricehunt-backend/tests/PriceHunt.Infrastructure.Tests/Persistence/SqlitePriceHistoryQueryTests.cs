using PriceHunt.Application.History;
using PriceHunt.Domain;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Infrastructure.Tests.Persistence;

public sealed class SqlitePriceHistoryQueryTests(HistoryDataset dataset) : IClassFixture<HistoryDataset>
{
    private static readonly DateTime s_start = HistoryDataset.Start;

    [Fact]
    [Trait("Requirement", "H1")]
    public async Task Returns_successful_quotes_newest_first_by_default()
    {
        PagedResult<PriceHistoryItem> page = await QueryAsync(new PriceHistoryFilter());

        Labels(page).Should().Equal("s3-alpha", "s2-charlie", "s2-alpha", "s1-bravo", "s1-alpha");
        page.TotalCount.Should().Be(5);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(PriceHistoryFilter.DefaultPageSize);
    }

    [Fact]
    [Trait("Requirement", "DB2")]
    public async Task Includes_failures_and_missing_responses_on_request()
    {
        PagedResult<PriceHistoryItem> page = await QueryAsync(new PriceHistoryFilter { IncludeFailures = true });

        page.TotalCount.Should().Be(7);
        PriceHistoryItem failed = page.Items.Single(item => Label(item) == "s1-charlie");
        failed.Outcome.Should().Be(ResponseOutcome.Failed);
        failed.ErrorCode.Should().Be("supplier_unavailable");
        failed.Price.Should().BeNull();
        page.Items.Single(item => Label(item) == "s2-bravo").Outcome.Should().Be(ResponseOutcome.TimedOut);
    }

    [Fact]
    [Trait("Requirement", "H1")]
    public async Task Filters_on_a_half_open_quote_time_range()
    {
        var filter = new PriceHistoryFilter { From = s_start.AddMilliseconds(1_200), To = s_start.AddMilliseconds(3_000) };

        PagedResult<PriceHistoryItem> page = await QueryAsync(filter);

        Labels(page).Should().Equal(["s1-alpha"], "the start is inclusive and the end exclusive");
    }

    [Theory]
    [Trait("Requirement", "H1")]
    [InlineData(new[] { "bravo" }, new[] { "s1-bravo" })]
    [InlineData(new[] { "alpha", "charlie" }, new[] { "s3-alpha", "s2-charlie", "s2-alpha", "s1-alpha" })]
    [InlineData(new string[0], new[] { "s3-alpha", "s2-charlie", "s2-alpha", "s1-bravo", "s1-alpha" })]
    public async Task Filters_by_one_several_or_all_suppliers(string[] supplierIds, string[] expected)
    {
        var filter = new PriceHistoryFilter { Suppliers = [.. supplierIds.Select(SupplierId.Create)] };

        PagedResult<PriceHistoryItem> page = await QueryAsync(filter);

        Labels(page).Should().Equal(expected);
    }

    [Theory]
    [Trait("Requirement", "H2")]
    [InlineData("haifa", null, new[] { "s1-bravo", "s1-alpha" })]
    [InlineData(null, "HAIFA", new[] { "s2-charlie", "s2-alpha" })]
    [InlineData("ZÜRICH", null, new[] { "s2-charlie", "s2-alpha" })]
    [InlineData(" otter ", null, new[] { "s3-alpha" })]
    [InlineData("rotterdam", "antwerp", new[] { "s3-alpha" })]
    public async Task Matches_locations_ignoring_case(string? origin, string? destination, string[] expected)
    {
        PagedResult<PriceHistoryItem> page = await QueryAsync(new PriceHistoryFilter { Origin = origin, Destination = destination });

        Labels(page).Should().Equal(expected);
    }

    [Theory]
    [Trait("Requirement", "HC2")]
    [InlineData(HistorySortField.Date, SortDirection.Ascending, new[] { "s1-alpha", "s1-bravo", "s2-alpha", "s2-charlie", "s3-alpha" })]
    [InlineData(HistorySortField.Date, SortDirection.Descending, new[] { "s3-alpha", "s2-charlie", "s2-alpha", "s1-bravo", "s1-alpha" })]
    [InlineData(HistorySortField.Route, SortDirection.Ascending, new[] { "s1-alpha", "s1-bravo", "s3-alpha", "s2-alpha", "s2-charlie" })]
    [InlineData(HistorySortField.Route, SortDirection.Descending, new[] { "s2-charlie", "s2-alpha", "s3-alpha", "s1-bravo", "s1-alpha" })]
    [InlineData(HistorySortField.Supplier, SortDirection.Ascending, new[] { "s1-alpha", "s2-alpha", "s3-alpha", "s1-bravo", "s2-charlie" })]
    [InlineData(HistorySortField.Supplier, SortDirection.Descending, new[] { "s2-charlie", "s1-bravo", "s3-alpha", "s2-alpha", "s1-alpha" })]
    [InlineData(HistorySortField.Price, SortDirection.Ascending, new[] { "s1-bravo", "s2-alpha", "s2-charlie", "s1-alpha", "s3-alpha" })]
    [InlineData(HistorySortField.Price, SortDirection.Descending, new[] { "s3-alpha", "s1-alpha", "s2-charlie", "s2-alpha", "s1-bravo" })]
    [InlineData(HistorySortField.ResponseTime, SortDirection.Ascending, new[] { "s2-alpha", "s3-alpha", "s1-alpha", "s1-bravo", "s2-charlie" })]
    [InlineData(HistorySortField.ResponseTime, SortDirection.Descending, new[] { "s2-charlie", "s1-bravo", "s1-alpha", "s3-alpha", "s2-alpha" })]
    public async Task Sorts_by_every_column_in_both_directions(HistorySortField sortBy, SortDirection direction, string[] expected)
    {
        PagedResult<PriceHistoryItem> page = await QueryAsync(new PriceHistoryFilter { SortBy = sortBy, SortDirection = direction });

        Labels(page).Should().Equal(expected);
    }

    [Theory]
    [Trait("Requirement", "HC2")]
    [InlineData(SortDirection.Ascending)]
    [InlineData(SortDirection.Descending)]
    public async Task Sorts_responses_without_a_price_last(SortDirection direction)
    {
        var filter = new PriceHistoryFilter { IncludeFailures = true, SortBy = HistorySortField.Price, SortDirection = direction };

        PagedResult<PriceHistoryItem> page = await QueryAsync(filter);

        page.Items.Take(5).Should().OnlyContain(item => item.Price != null);
        page.Items.Skip(5).Should().HaveCount(2).And.OnlyContain(item => item.Price == null);
    }

    [Theory]
    [Trait("Requirement", "HC2")]
    [InlineData(1, new[] { "s1-alpha", "s1-bravo" })]
    [InlineData(3, new[] { "s3-alpha" })]
    [InlineData(4, new string[0])]
    public async Task Pages_through_the_results(int pageNumber, string[] expected)
    {
        var filter = new PriceHistoryFilter { SortBy = HistorySortField.Date, SortDirection = SortDirection.Ascending, Page = pageNumber, PageSize = 2 };

        PagedResult<PriceHistoryItem> page = await QueryAsync(filter);

        Labels(page).Should().Equal(expected);
        page.TotalCount.Should().Be(5, "the total counts every match, whatever the page");
        page.Page.Should().Be(pageNumber);
    }

    [Fact]
    [Trait("Requirement", "HC2")]
    public async Task A_page_past_the_32_bit_offset_range_is_empty()
    {
        // (1 073 741 825 − 1) × 100 wraps round to an offset of exactly 0 in 32-bit arithmetic.
        PagedResult<PriceHistoryItem> page = await QueryAsync(new PriceHistoryFilter { Page = 1_073_741_825, PageSize = 100 });

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(5);
        page.Page.Should().Be(1_073_741_825);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, PriceHistoryFilter.MaxPageSize + 1)]
    public async Task Rejects_an_invalid_page(int pageNumber, int pageSize)
    {
        Func<Task> query = () => QueryAsync(new PriceHistoryFilter { Page = pageNumber, PageSize = pageSize });

        await query.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    [Trait("Requirement", "HC2")]
    public async Task Returns_every_column_of_the_table()
    {
        PagedResult<PriceHistoryItem> page = await QueryAsync(new PriceHistoryFilter { Suppliers = [HistoryDataset.Bravo] });

        PriceHistoryItem item = page.Items.Should().ContainSingle().Subject;
        item.ReceivedAt.Should().Be(s_start.AddMilliseconds(3_000));
        item.ReceivedAt.Kind.Should().Be(DateTimeKind.Utc);
        item.Origin.Should().Be("Haifa");
        item.Destination.Should().Be("Rotterdam");
        item.ShipDateFrom.Should().Be(new DateOnly(2026, 10, 1));
        item.ShipDateTo.Should().Be(new DateOnly(2026, 10, 8));
        item.SupplierId.Should().Be("bravo");
        item.SupplierName.Should().Be("Bravo Cargo");
        item.Price.Should().Be(Money.Create(900.50m, "USD"));
        item.ResponseTime.Should().Be(TimeSpan.FromSeconds(3));
        item.Outcome.Should().Be(ResponseOutcome.Succeeded);
    }

    private Task<PagedResult<PriceHistoryItem>> QueryAsync(PriceHistoryFilter filter) =>
        new SqlitePriceHistoryQuery(dataset.Database).GetAsync(filter, TestContext.Current.CancellationToken);

    private string Label(PriceHistoryItem item) => dataset.LabelOf(item.Id);

    private string[] Labels(PagedResult<PriceHistoryItem> page) => [.. page.Items.Select(Label)];
}
