using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceHunt.Domain;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Api.Tests;

public sealed class SearchStreamingTests
{
    private static readonly string[] s_copperAndAurora = ["copper", "aurora"];

    private static readonly object s_validSearch = new
    {
        origin = "Haifa",
        destination = "Rotterdam",
        fromDate = "2026-10-01",
        toDate = "2026-10-08",
    };

    [Fact]
    [Trait("Requirement", "SV1")]
    public async Task Streams_search_started_first_and_exactly_one_terminal_event_last()
    {
        await using var factory = new StreamingApiFactory();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await StartSearchAsync(client, s_validSearch);
        await using var events = new ServerSentEventReader(await response.Content.ReadAsStreamAsync(Token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
        ReceivedEvent started = await events.NextAsync();
        factory.Bluefin.Respond(950m);
        ReceivedEvent first = await events.NextAsync();
        factory.Aurora.Fail(new InvalidOperationException("Broken."));
        ReceivedEvent second = await events.NextAsync();
        factory.Copper.Respond(1200m);
        ReceivedEvent third = await events.NextAsync();
        ReceivedEvent completed = await events.NextAsync();

        new[] { started.Type, first.Type, second.Type, third.Type, completed.Type }
            .Should().Equal("search-started", "quote-received", "supplier-failed", "quote-received", "search-completed");
        new[] { started.Id, first.Id, second.Id, third.Id, completed.Id }.Should().Equal("1", "2", "3", "4", "5");
        (await events.EndedAsync()).Should().BeTrue("nothing follows the terminal event");
        completed.GetString("status").Should().Be("Completed");
        completed.Data.GetProperty("respondedCount").GetInt32().Should().Be(3);
        completed.Data.GetProperty("failedCount").GetInt32().Should().Be(1);
    }

    [Fact]
    [Trait("Requirement", "SV1")]
    public async Task Delivers_the_first_quote_while_other_suppliers_are_still_pending()
    {
        await using var factory = new StreamingApiFactory();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await StartSearchAsync(client, s_validSearch);
        await using var events = new ServerSentEventReader(await response.Content.ReadAsStreamAsync(Token));
        await events.NextAsync();

        factory.Copper.Respond(700m);
        ReceivedEvent quote = await events.NextAsync();

        quote.Type.Should().Be("quote-received");
        quote.GetString("supplierId").Should().Be("copper");
        factory.Aurora.CancellationObserved.IsCompleted.Should().BeFalse("aurora is still being waited for");
        factory.Bluefin.CancellationObserved.IsCompleted.Should().BeFalse("bluefin is still being waited for");
    }

    [Fact]
    [Trait("Requirement", "SV1")]
    public async Task Writes_camel_case_json_with_enum_names_utc_timestamps_and_money_objects()
    {
        await using var factory = new StreamingApiFactory();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await StartSearchAsync(client, s_validSearch);
        await using var events = new ServerSentEventReader(await response.Content.ReadAsStreamAsync(Token));

        ReceivedEvent started = await events.NextAsync();
        factory.Aurora.Respond(1234.56m);
        ReceivedEvent quote = await events.NextAsync();

        started.Data.GetProperty("suppliers").EnumerateArray().Select(supplier => supplier.GetProperty("name").GetString())
            .Should().Equal("Aurora Freight", "Bluefin Cargo", "Copper Lines");
        started.GetString("startedAt").Should().Be("2026-09-30T10:00:00Z");
        started.GetString("deadline").Should().Be("2026-09-30T10:00:06Z");
        started.Data.GetProperty("maxDurationMs").GetInt64().Should().Be(6_000);
        quote.GetString("searchId").Should().Be(started.GetString("searchId"));
        quote.Data.GetProperty("price").GetProperty("amount").GetDecimal().Should().Be(1234.56m);
        quote.Data.GetProperty("price").GetProperty("currency").GetString().Should().Be("USD");
        quote.Data.GetProperty("responseTimeMs").GetInt64().Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "SV3")]
    public async Task Ends_as_timed_out_at_the_deadline_and_records_the_silent_suppliers()
    {
        await using var factory = new StreamingApiFactory();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await StartSearchAsync(client, s_validSearch);
        await using var events = new ServerSentEventReader(await response.Content.ReadAsStreamAsync(Token));
        ReceivedEvent started = await events.NextAsync();
        factory.Aurora.Respond(800m);
        await events.NextAsync();

        factory.Time.Advance(TimeSpan.FromSeconds(6));
        ReceivedEvent completed = await events.NextAsync();

        completed.Type.Should().Be("search-completed");
        completed.GetString("status").Should().Be("TimedOut");
        completed.Data.GetProperty("noResponseSupplierIds").EnumerateArray().Select(id => id.GetString()).Should().Equal("bluefin", "copper");
        (await events.EndedAsync()).Should().BeTrue();
        await Task.WhenAll(factory.Bluefin.CancellationObserved, factory.Copper.CancellationObserved).WaitAsync(Guard, Token);

        SearchEntity stored = await LoadSearchAsync(factory, Guid.Parse(started.GetString("searchId")));
        stored.Status.Should().Be(SearchStatus.TimedOut);
        stored.Responses.Where(row => row.Outcome == ResponseOutcome.TimedOut).Select(row => row.SupplierId)
            .Should().BeEquivalentTo(["bluefin", "copper"]);
    }

    [Fact]
    [Trait("Requirement", "SV5")]
    public async Task Aborting_the_request_cancels_the_suppliers_and_records_the_search_as_cancelled()
    {
        await using var factory = new StreamingApiFactory();
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await StartSearchAsync(client, s_validSearch);
        var events = new ServerSentEventReader(await response.Content.ReadAsStreamAsync(Token));
        ReceivedEvent started = await events.NextAsync();
        factory.Aurora.Respond(640m);
        await events.NextAsync();

        await events.DisposeAsync();
        response.Dispose();

        await Task.WhenAll(factory.Bluefin.CancellationObserved, factory.Copper.CancellationObserved).WaitAsync(Guard, Token);
        (await factory.Repository.OutcomeSaved.WaitAsync(Guard, Token)).Should().Be(SearchStatus.Cancelled);
        SearchEntity stored = await LoadSearchAsync(factory, Guid.Parse(started.GetString("searchId")));
        stored.Status.Should().Be(SearchStatus.Cancelled);
        stored.Responses.Select(row => (row.SupplierId, row.Outcome)).Should().BeEquivalentTo(
            [("aurora", ResponseOutcome.Succeeded), ("bluefin", ResponseOutcome.Cancelled), ("copper", ResponseOutcome.Cancelled)]);
    }

    [Fact]
    [Trait("Requirement", "P1")]
    public async Task Queries_only_the_selected_suppliers()
    {
        await using var factory = new StreamingApiFactory();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await StartSearchAsync(client, new
        {
            origin = "Haifa",
            destination = "Rotterdam",
            fromDate = "2026-10-01",
            toDate = "2026-10-08",
            supplierIds = s_copperAndAurora,
        });
        await using var events = new ServerSentEventReader(await response.Content.ReadAsStreamAsync(Token));

        ReceivedEvent started = await events.NextAsync();

        started.Data.GetProperty("suppliers").EnumerateArray().Select(supplier => supplier.GetProperty("id").GetString())
            .Should().Equal("aurora", "copper");
    }

    [Theory]
    [InlineData("""{ "destination": "Rotterdam", "fromDate": "2026-10-01", "toDate": "2026-10-08" }""", "origin")]
    [InlineData("""{ "origin": "Haifa", "destination": "HAIFA", "fromDate": "2026-10-01", "toDate": "2026-10-08" }""", "destination")]
    [InlineData("""{ "origin": "Haifa", "destination": "Rotterdam", "fromDate": "2026-10-08", "toDate": "2026-10-01" }""", "toDate")]
    [InlineData("""{ "origin": "Haifa", "destination": "Rotterdam", "fromDate": "2026-10-01", "toDate": "2026-10-08", "supplierIds": ["zephyr"] }""", "supplierIds")]
    public async Task Rejects_an_invalid_search_with_field_errors_before_streaming(string body, string invalidField)
    {
        await using var factory = new StreamingApiFactory();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsync("/api/searches", new StringContent(body, System.Text.Encoding.UTF8, "application/json"), Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token));
        problem.RootElement.GetProperty("errors").EnumerateObject().Select(error => error.Name).Should().Contain(invalidField);
    }

    [Fact]
    public async Task Rejects_malformed_json_with_a_problem()
    {
        await using var factory = new StreamingApiFactory();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsync("/api/searches", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"), Token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static TimeSpan Guard => TimeSpan.FromSeconds(10);

    private static async Task<HttpResponseMessage> StartSearchAsync(HttpClient client, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/searches") { Content = JsonContent.Create(body) };
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);
    }

    private static async Task<SearchEntity> LoadSearchAsync(StreamingApiFactory factory, Guid searchId)
    {
        IDbContextFactory<PriceHuntDbContext> contexts = factory.Services.GetRequiredService<IDbContextFactory<PriceHuntDbContext>>();
        await using PriceHuntDbContext db = await contexts.CreateDbContextAsync(Token);
        return await db.Searches.AsNoTracking().Include(search => search.Responses).SingleAsync(search => search.Id == searchId, Token);
    }
}
