namespace PriceHunt.Domain.Tests;

public sealed class SearchTests
{
    private static readonly DateTime s_createdAt = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
    private static readonly SupplierId s_aurora = SupplierId.Create("aurora");
    private static readonly SupplierId s_bluefin = SupplierId.Create("bluefin");
    private static readonly SupplierId s_copper = SupplierId.Create("copper");

    private static readonly SearchCriteria s_criteria = new(
        Route.Create(Location.Create("Haifa"), Location.Create("Rotterdam")),
        ShippingDateRange.Create(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8)));

    private static Search StartSearch() => Search.Start(s_criteria, [s_aurora, s_bluefin, s_copper], s_createdAt);

    [Fact]
    public void Starts_running_with_every_selected_supplier_pending()
    {
        Search search = StartSearch();

        search.Status.Should().Be(SearchStatus.Running);
        search.CreatedAt.Should().Be(s_createdAt);
        search.CompletedAt.Should().BeNull();
        search.SelectedSuppliers.Should().Equal(s_aurora, s_bluefin, s_copper);
        search.PendingSuppliers.Should().Equal(s_aurora, s_bluefin, s_copper);
        search.Responses.Should().BeEmpty();
    }

    [Fact]
    public void Gets_a_time_ordered_id()
    {
        var earlier = Search.Start(s_criteria, [s_aurora], s_createdAt);
        var later = Search.Start(s_criteria, [s_aurora], s_createdAt.AddMilliseconds(1));

        earlier.Id.Version.Should().Be(7);
        string.CompareOrdinal(earlier.Id.ToString(), later.Id.ToString()).Should().BeNegative();
    }

    [Fact]
    public void Requires_at_least_one_supplier()
    {
        Action start = () => Search.Start(s_criteria, [], s_createdAt);

        start.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rejects_duplicate_suppliers()
    {
        Action start = () => Search.Start(s_criteria, [s_aurora, s_aurora], s_createdAt);

        start.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Requires_utc_timestamps()
    {
        Action start = () => Search.Start(s_criteria, [s_aurora], new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Local));

        start.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Records_a_quote_for_a_selected_supplier()
    {
        Search search = StartSearch();

        SupplierResponse response = search.RecordQuote(s_bluefin, Money.Create(950m, "USD"), TimeSpan.FromSeconds(1.2), s_createdAt.AddSeconds(1.2));

        response.Outcome.Should().Be(ResponseOutcome.Succeeded);
        response.SupplierId.Should().Be(s_bluefin);
        response.Price.Should().Be(Money.Create(950m, "USD"));
        response.ResponseTime.Should().Be(TimeSpan.FromSeconds(1.2));
        response.ReceivedAt.Should().Be(s_createdAt.AddSeconds(1.2));
        response.ErrorCode.Should().BeNull();
        search.Responses.Should().ContainSingle().Which.Should().BeSameAs(response);
        search.PendingSuppliers.Should().Equal(s_aurora, s_copper);
    }

    [Fact]
    public void Records_a_failure_with_its_error()
    {
        Search search = StartSearch();

        SupplierResponse response = search.RecordFailure(s_aurora, "supplier_unavailable", "Try again later.", TimeSpan.FromSeconds(2), s_createdAt.AddSeconds(2));

        response.Outcome.Should().Be(ResponseOutcome.Failed);
        response.Price.Should().BeNull();
        response.ErrorCode.Should().Be("supplier_unavailable");
        response.ErrorMessage.Should().Be("Try again later.");
    }

    [Fact]
    public void Rejects_a_response_from_a_supplier_that_was_not_selected()
    {
        var search = Search.Start(s_criteria, [s_aurora], s_createdAt);

        Action record = () => search.RecordQuote(s_bluefin, Money.Create(1m, "USD"), TimeSpan.Zero, s_createdAt);

        record.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rejects_a_second_response_from_the_same_supplier()
    {
        Search search = StartSearch();
        search.RecordQuote(s_aurora, Money.Create(1m, "USD"), TimeSpan.Zero, s_createdAt);

        Action record = () => search.RecordFailure(s_aurora, "late", "Late.", TimeSpan.Zero, s_createdAt);

        record.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rejects_a_negative_response_time()
    {
        Search search = StartSearch();

        Action record = () => search.RecordQuote(s_aurora, Money.Create(1m, "USD"), TimeSpan.FromMilliseconds(-1), s_createdAt);

        record.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Completes_once_every_supplier_has_responded()
    {
        var search = Search.Start(s_criteria, [s_aurora, s_bluefin], s_createdAt);
        search.RecordQuote(s_aurora, Money.Create(1m, "USD"), TimeSpan.FromSeconds(1), s_createdAt.AddSeconds(1));
        search.RecordFailure(s_bluefin, "boom", "Boom.", TimeSpan.FromSeconds(2), s_createdAt.AddSeconds(2));

        search.Complete(s_createdAt.AddSeconds(2));

        search.Status.Should().Be(SearchStatus.Completed);
        search.CompletedAt.Should().Be(s_createdAt.AddSeconds(2));
        search.HasPendingSuppliers.Should().BeFalse();
    }

    [Fact]
    public void Cannot_complete_while_suppliers_are_pending()
    {
        Search search = StartSearch();

        Action complete = () => search.Complete(s_createdAt.AddSeconds(1));

        complete.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Timing_out_records_every_pending_supplier_as_timed_out()
    {
        Search search = StartSearch();
        search.RecordQuote(s_bluefin, Money.Create(1m, "USD"), TimeSpan.FromSeconds(1), s_createdAt.AddSeconds(1));
        DateTime deadline = s_createdAt.AddSeconds(6);

        IReadOnlyList<SupplierResponse> closing = search.TimeOut(deadline);

        search.Status.Should().Be(SearchStatus.TimedOut);
        search.CompletedAt.Should().Be(deadline);
        closing.Select(response => response.SupplierId).Should().Equal(s_aurora, s_copper);
        closing.Should().AllSatisfy(response =>
        {
            response.Outcome.Should().Be(ResponseOutcome.TimedOut);
            response.Price.Should().BeNull();
            response.ResponseTime.Should().Be(TimeSpan.FromSeconds(6));
            response.ReceivedAt.Should().Be(deadline);
        });
        search.Responses.Should().HaveCount(3);
        search.HasPendingSuppliers.Should().BeFalse();
    }

    [Fact]
    public void Cancelling_records_every_pending_supplier_as_cancelled()
    {
        Search search = StartSearch();

        IReadOnlyList<SupplierResponse> closing = search.Cancel(s_createdAt.AddSeconds(2));

        search.Status.Should().Be(SearchStatus.Cancelled);
        closing.Should().HaveCount(3).And.OnlyContain(response => response.Outcome == ResponseOutcome.Cancelled);
    }

    [Fact]
    public void Faulting_records_every_pending_supplier_as_cancelled()
    {
        Search search = StartSearch();
        search.RecordQuote(s_aurora, Money.Create(1m, "USD"), TimeSpan.FromSeconds(1), s_createdAt.AddSeconds(1));

        IReadOnlyList<SupplierResponse> closing = search.Fault(s_createdAt.AddSeconds(3));

        search.Status.Should().Be(SearchStatus.Faulted);
        closing.Select(response => response.SupplierId).Should().Equal(s_bluefin, s_copper);
        closing.Should().OnlyContain(response => response.Outcome == ResponseOutcome.Cancelled);
    }

    public static TheoryData<string> TerminalTransitions => ["complete", "time out", "cancel", "fault"];

    [Theory]
    [MemberData(nameof(TerminalTransitions))]
    public void A_terminal_state_is_final(string transition)
    {
        var search = Search.Start(s_criteria, [s_aurora], s_createdAt);
        search.RecordQuote(s_aurora, Money.Create(1m, "USD"), TimeSpan.FromSeconds(1), s_createdAt.AddSeconds(1));
        Apply(search, transition);

        search.Status.Should().NotBe(SearchStatus.Running);
        foreach (string next in TerminalTransitions)
        {
            Action again = () => Apply(search, next);
            again.Should().Throw<InvalidOperationException>($"'{next}' after '{transition}' must be rejected");
        }

        Action record = () => search.RecordFailure(s_aurora, "late", "Late.", TimeSpan.Zero, s_createdAt);
        record.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rejects_a_completion_time_before_the_start()
    {
        var search = Search.Start(s_criteria, [s_aurora], s_createdAt);

        Action cancel = () => search.Cancel(s_createdAt.AddSeconds(-1));

        cancel.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Restores_a_persisted_search_with_its_responses()
    {
        Search original = StartSearch();
        SupplierResponse quote = original.RecordQuote(s_aurora, Money.Create(10m, "USD"), TimeSpan.FromSeconds(1), s_createdAt.AddSeconds(1));

        var restored = Search.Restore(original.Id, s_criteria, original.SelectedSuppliers, s_createdAt, SearchStatus.Running, null, [quote]);

        restored.Id.Should().Be(original.Id);
        restored.Status.Should().Be(SearchStatus.Running);
        restored.PendingSuppliers.Should().Equal(s_bluefin, s_copper);
        restored.Cancel(s_createdAt.AddSeconds(5)).Should().HaveCount(2);
    }

    [Fact]
    public void Restore_rejects_a_response_from_an_unselected_supplier()
    {
        SupplierResponse stray = Search.Start(s_criteria, [s_copper], s_createdAt)
            .RecordQuote(s_copper, Money.Create(1m, "USD"), TimeSpan.Zero, s_createdAt);

        Action restore = () => Search.Restore(Guid.CreateVersion7(), s_criteria, [s_aurora], s_createdAt, SearchStatus.Running, null, [stray]);

        restore.Should().Throw<InvalidOperationException>();
    }

    private static void Apply(Search search, string transition)
    {
        DateTime at = s_createdAt.AddSeconds(6);
        switch (transition)
        {
            case "complete":
                search.Complete(at);
                break;
            case "time out":
                search.TimeOut(at);
                break;
            case "cancel":
                search.Cancel(at);
                break;
            default:
                search.Fault(at);
                break;
        }
    }
}
