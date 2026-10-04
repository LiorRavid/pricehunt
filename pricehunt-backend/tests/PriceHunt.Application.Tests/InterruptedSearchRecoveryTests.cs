using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PriceHunt.Application.Searches;
using PriceHunt.Application.Tests.Fakes;
using PriceHunt.Domain;

namespace PriceHunt.Application.Tests;

public sealed class InterruptedSearchRecoveryTests
{
    private static readonly DateTime s_createdAt = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan s_maxDuration = TimeSpan.FromSeconds(6);

    private static readonly SearchCriteria s_criteria = new(
        Route.Create(Location.Create("Haifa"), Location.Create("Rotterdam")),
        ShippingDateRange.Create(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8)));

    private readonly InMemorySearchRepository _repository = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(s_createdAt.AddMinutes(5)));

    [Fact]
    [Trait("Requirement", "DB1")]
    public async Task Closes_searches_left_running_as_cancelled()
    {
        var aurora = SupplierId.Create("aurora");
        var bluefin = SupplierId.Create("bluefin");
        var interrupted = Search.Start(s_criteria, [aurora, bluefin], s_createdAt);
        interrupted.RecordQuote(aurora, Money.Create(10m, "USD"), TimeSpan.FromSeconds(1), s_createdAt.AddSeconds(1));
        _repository.Running.Add(interrupted);

        int recovered = await Recovery().RecoverAsync(TestContext.Current.CancellationToken);

        recovered.Should().Be(1);
        SavedOutcome outcome = _repository.Outcomes.Should().ContainSingle().Subject;
        outcome.Status.Should().Be(SearchStatus.Cancelled);
        outcome.CompletedAt.Should().Be(s_createdAt + s_maxDuration, "the restart came 5 minutes later, past the deadline");
        outcome.ClosingResponses.Should().ContainSingle().Which.SupplierId.Should().Be(bluefin);
    }

    [Fact]
    [Trait("Requirement", "DB2")]
    public async Task Closes_at_the_deadline_when_the_restart_comes_later()
    {
        var aurora = SupplierId.Create("aurora");
        _repository.Running.Add(Search.Start(s_criteria, [aurora], s_createdAt));

        await Recovery().RecoverAsync(TestContext.Current.CancellationToken);

        SavedOutcome outcome = _repository.Outcomes.Should().ContainSingle().Subject;
        outcome.CompletedAt.Should().Be(s_createdAt + s_maxDuration, "a search can't outlive its deadline");
        SupplierResponse closed = outcome.ClosingResponses.Should().ContainSingle().Subject;
        closed.ResponseTime.Should().Be(s_maxDuration, "the downtime isn't a response time");
        closed.ReceivedAt.Should().Be(s_createdAt + s_maxDuration);
    }

    [Fact]
    [Trait("Requirement", "DB1")]
    public async Task Completes_a_search_whose_suppliers_had_all_answered()
    {
        var aurora = SupplierId.Create("aurora");
        var answered = Search.Start(s_criteria, [aurora], s_createdAt);
        answered.RecordQuote(aurora, Money.Create(10m, "USD"), TimeSpan.FromSeconds(1), s_createdAt.AddSeconds(1));
        _repository.Running.Add(answered);

        await Recovery().RecoverAsync(TestContext.Current.CancellationToken);

        SavedOutcome outcome = _repository.Outcomes.Should().ContainSingle().Subject;
        outcome.Status.Should().Be(SearchStatus.Completed);
        outcome.ClosingResponses.Should().BeEmpty();
    }

    [Fact]
    public async Task Uses_the_start_time_when_the_clock_is_behind_it()
    {
        var future = Search.Start(s_criteria, [SupplierId.Create("aurora")], s_createdAt.AddHours(1));
        _repository.Running.Add(future);

        await Recovery().RecoverAsync(TestContext.Current.CancellationToken);

        _repository.Outcomes.Single().CompletedAt.Should().Be(s_createdAt.AddHours(1));
    }

    [Fact]
    public async Task Does_nothing_when_no_search_is_running()
    {
        int recovered = await Recovery().RecoverAsync(TestContext.Current.CancellationToken);

        recovered.Should().Be(0);
        _repository.Outcomes.Should().BeEmpty();
    }

    private InterruptedSearchRecovery Recovery() =>
        new(_repository, _time, Options.Create(new SearchOptions { MaxDuration = s_maxDuration }), NullLogger<InterruptedSearchRecovery>.Instance);
}
