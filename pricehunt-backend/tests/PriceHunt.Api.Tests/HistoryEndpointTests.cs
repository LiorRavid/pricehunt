using System.Net;
using System.Text.Json;

namespace PriceHunt.Api.Tests;

public sealed class HistoryEndpointTests(HistoryApiFixture fixture) : IClassFixture<HistoryApiFixture>
{
    [Fact]
    [Trait("Requirement", "H1")]
    public async Task Returns_a_page_of_successful_quotes_newest_first()
    {
        using JsonDocument page = await GetPageAsync("/api/history");

        page.RootElement.GetProperty("totalCount").GetInt32().Should().Be(3);
        page.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        page.RootElement.GetProperty("pageSize").GetInt32().Should().Be(20);
        Prices(page).Should().Equal(1200m, 990.25m, 1500m);
    }

    [Fact]
    [Trait("Requirement", "H1")]
    public async Task Binds_a_repeated_suppliers_parameter()
    {
        using JsonDocument page = await GetPageAsync("/api/history?suppliers=bramblewood-cargo&suppliers=driftwood-shipping&includeFailures=true");

        Items(page).Select(item => item.GetProperty("supplierId").GetString())
            .Should().BeEquivalentTo(["bramblewood-cargo", "driftwood-shipping"]);
    }

    [Fact]
    [Trait("Requirement", "H1")]
    public async Task Filters_on_a_half_open_utc_range()
    {
        string start = Uri.EscapeDataString(HistoryApiFixture.Start.AddMilliseconds(1_200).ToString("O"));
        string end = Uri.EscapeDataString(HistoryApiFixture.Start.AddMilliseconds(2_500).ToString("O"));

        using JsonDocument page = await GetPageAsync($"/api/history?startDate={start}&endDate={end}");

        Prices(page).Should().Equal(1500m);
    }

    [Fact]
    [Trait("Requirement", "H2")]
    public async Task Filters_by_origin_and_destination_ignoring_case()
    {
        using JsonDocument byOrigin = await GetPageAsync("/api/history?origin=HAIFA");
        using JsonDocument byDestination = await GetPageAsync("/api/history?destination=haifa");

        Items(byOrigin).Should().HaveCount(2).And.OnlyContain(item => item.GetProperty("origin").GetString() == "Haifa");
        Items(byDestination).Should().ContainSingle().Which.GetProperty("origin").GetString().Should().Be("Antwerp");
    }

    [Fact]
    [Trait("Requirement", "HC2")]
    public async Task Sorts_and_pages_as_asked()
    {
        using JsonDocument firstPage = await GetPageAsync("/api/history?sortBy=price&sortDirection=asc&page=1&pageSize=2");
        using JsonDocument lastPage = await GetPageAsync("/api/history?sortBy=PRICE&sortDirection=ASC&page=2&pageSize=2");

        Prices(firstPage).Should().Equal(990.25m, 1200m);
        Prices(lastPage).Should().Equal(1500m);
        lastPage.RootElement.GetProperty("totalCount").GetInt32().Should().Be(3);
    }

    [Fact]
    [Trait("Requirement", "DB2")]
    public async Task Shows_failures_with_their_outcome_and_no_price_on_request()
    {
        using JsonDocument page = await GetPageAsync("/api/history?includeFailures=true&sortBy=date&sortDirection=asc");

        JsonElement failure = Items(page).Single(item => item.GetProperty("outcome").GetString() == "Failed");
        failure.GetProperty("price").ValueKind.Should().Be(JsonValueKind.Null);
        failure.GetProperty("errorCode").GetString().Should().Be("supplier_unavailable");
        failure.GetProperty("supplierName").GetString().Should().Be("Driftwood Shipping");
        failure.GetProperty("receivedAt").GetString().Should().Be("2026-09-20T08:00:03Z");
        failure.GetProperty("responseTimeMs").GetInt64().Should().Be(3_000);
        failure.GetProperty("shipDateFrom").GetString().Should().Be("2026-10-01");
    }

    [Theory]
    [InlineData("sortBy=cheapest", "sortBy")]
    [InlineData("sortDirection=up", "sortDirection")]
    [InlineData("page=0", "page")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("suppliers=Not%20A%20Slug", "suppliers")]
    [InlineData("startDate=2026-09-21T00:00:00Z&endDate=2026-09-20T00:00:00Z", "endDate")]
    public async Task Rejects_invalid_parameters_with_field_errors(string query, string invalidField)
    {
        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage response = await client.GetAsync($"/api/history?{query}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        problem.RootElement.GetProperty("errors").EnumerateObject().Select(error => error.Name).Should().Contain(invalidField);
    }

    [Theory]
    [InlineData("page=abc")]
    [InlineData("startDate=yesterday")]
    public async Task Rejects_unparseable_parameters_with_a_problem(string query)
    {
        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage response = await client.GetAsync($"/api/history?{query}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task<JsonDocument> GetPageAsync(string url)
    {
        using HttpClient client = fixture.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static JsonElement[] Items(JsonDocument page) => [.. page.RootElement.GetProperty("items").EnumerateArray()];

    private static decimal[] Prices(JsonDocument page) =>
        [.. Items(page).Select(item => item.GetProperty("price").GetProperty("amount").GetDecimal())];
}
