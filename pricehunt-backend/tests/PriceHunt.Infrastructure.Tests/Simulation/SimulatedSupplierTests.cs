using Microsoft.Extensions.Time.Testing;
using PriceHunt.Domain;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Infrastructure.Tests.Simulation;

public sealed class SimulatedSupplierTests
{
    private static readonly SearchCriteria s_criteria = new(
        Route.Create(Location.Create("Haifa"), Location.Create("Rotterdam")),
        ShippingDateRange.Create(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8)));

    private static readonly SimulatedSupplierOptions s_options = new()
    {
        Id = "albatross-freight",
        Name = "Albatross Freight",
        MinPrice = 1000m,
        MaxPrice = 2000m,
    };

    private readonly FakeTimeProvider _time = new();

    [Theory]
    [Trait("Requirement", "S2")]
    [InlineData(0.0, 500)]
    [InlineData(0.999999999, 5_000)]
    public void Draws_delays_between_half_a_second_and_five_seconds(double sample, int expectedMilliseconds)
    {
        QuoteGenerator generator = Generator(new ScriptedRandomSource(sample));

        TimeSpan delay = generator.NextDelay();

        delay.Should().Be(TimeSpan.FromMilliseconds(expectedMilliseconds));
    }

    [Fact]
    [Trait("Requirement", "S2")]
    public void Every_seeded_delay_stays_within_the_bounds()
    {
        QuoteGenerator generator = Generator(new SeededRandomSource(20260930));

        TimeSpan[] delays = [.. Enumerable.Range(0, 10_000).Select(_ => generator.NextDelay())];

        delays.Should().OnlyContain(delay => delay >= TimeSpan.FromMilliseconds(500) && delay <= TimeSpan.FromSeconds(5));
        delays.Distinct().Count().Should().BeGreaterThan(1_000, "the delay is random, not constant");
    }

    [Fact]
    [Trait("Requirement", "S2")]
    public void Prices_stay_within_the_supplier_range_with_two_decimals()
    {
        QuoteGenerator generator = Generator(new SeededRandomSource(7));

        Money[] prices = [.. Enumerable.Range(0, 5_000).Select(_ => generator.NextPrice())];

        prices.Should().OnlyContain(price => price.Amount >= 1000m && price.Amount <= 2000m && price.Currency == "USD");
        prices.Should().OnlyContain(price => decimal.Round(price.Amount, 2) == price.Amount);
    }

    [Fact]
    [Trait("Requirement", "S2")]
    public async Task A_quote_arrives_exactly_at_the_drawn_delay()
    {
        var supplier = new ReliableSupplier(s_options, Generator(new ScriptedRandomSource(0.26)), _time);

        Task<Money> quote = supplier.GetQuoteAsync(s_criteria, TestContext.Current.CancellationToken);
        var drawn = TimeSpan.FromMilliseconds(500 + Math.Floor(0.26 * 4_501));
        _time.Advance(drawn - TimeSpan.FromMilliseconds(1));
        quote.IsCompleted.Should().BeFalse();
        _time.Advance(TimeSpan.FromMilliseconds(1));

        Money price = await quote.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        price.Amount.Should().BeInRange(1000m, 2000m);
    }

    [Fact]
    [Trait("Requirement", "SV5")]
    public async Task Cancellation_mid_delay_stops_the_supplier_promptly()
    {
        var supplier = new ReliableSupplier(s_options, Generator(new ScriptedRandomSource(0.9)), _time);
        using var cancellation = new CancellationTokenSource();
        Task<Money> quote = supplier.GetQuoteAsync(s_criteria, cancellation.Token);
        _time.Advance(TimeSpan.FromMilliseconds(100));

        await cancellation.CancelAsync();

        Func<Task> waiting = () => quote.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await waiting.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [Trait("Requirement", "S3")]
    [InlineData(0.29, true)]
    [InlineData(0.30, false)]
    public void The_failure_decision_compares_a_draw_with_the_failure_rate(double sample, bool expected)
    {
        FailureDecision.ShouldFail(new ScriptedRandomSource(sample), failureRate: 0.3).Should().Be(expected);
    }

    [Fact]
    [Trait("Requirement", "S3")]
    public void Fails_about_thirty_percent_of_the_time()
    {
        var random = new SeededRandomSource(42);

        int failures = Enumerable.Range(0, 10_000).Count(_ => FailureDecision.ShouldFail(random, failureRate: 0.3));

        (failures / 10_000.0).Should().BeApproximately(0.30, 0.02);
    }

    [Fact]
    [Trait("Requirement", "S3")]
    public async Task A_flaky_supplier_fails_after_its_delay_when_the_decision_says_so()
    {
        var supplier = new FlakySupplier(s_options with { FailureRate = 0.3 }, Generator(new ScriptedRandomSource(0.0)), _time);

        Task<Money> quote = supplier.GetQuoteAsync(s_criteria, TestContext.Current.CancellationToken);
        quote.IsCompleted.Should().BeFalse("the failure comes after the delay");
        _time.Advance(TimeSpan.FromMilliseconds(500));

        Func<Task> waiting = () => quote.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        (await waiting.Should().ThrowAsync<SupplierUnavailableException>())
            .Which.ErrorCode.Should().Be("supplier_unavailable");
    }

    [Fact]
    [Trait("Requirement", "S3")]
    public async Task A_flaky_supplier_quotes_when_the_decision_says_not_to_fail()
    {
        var supplier = new FlakySupplier(s_options with { FailureRate = 0.3 }, Generator(new ScriptedRandomSource(0.5)), _time);

        Task<Money> quote = supplier.GetQuoteAsync(s_criteria, TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromSeconds(5));

        (await quote.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Currency.Should().Be("USD");
    }

    [Fact]
    [Trait("Requirement", "S4")]
    public async Task The_unresponsive_supplier_completes_only_through_cancellation()
    {
        var supplier = new UnresponsiveSupplier(s_options, _time);
        using var cancellation = new CancellationTokenSource();

        Task<Money> quote = supplier.GetQuoteAsync(s_criteria, cancellation.Token);
        _time.Advance(TimeSpan.FromDays(365));
        quote.IsCompleted.Should().BeFalse("the unresponsive supplier never answers on its own");
        await cancellation.CancelAsync();

        Func<Task> waiting = () => quote.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await waiting.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Exposes_its_configured_identity()
    {
        var supplier = new UnresponsiveSupplier(s_options, _time);

        supplier.Id.Should().Be(SupplierId.Create("albatross-freight"));
        supplier.DisplayName.Should().Be("Albatross Freight");
    }

    private static QuoteGenerator Generator(IRandomSource random) =>
        new(random, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5), 1000m, 2000m);
}
