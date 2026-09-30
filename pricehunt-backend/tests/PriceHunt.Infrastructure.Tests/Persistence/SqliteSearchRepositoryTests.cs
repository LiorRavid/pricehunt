using Microsoft.EntityFrameworkCore;
using PriceHunt.Domain;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Infrastructure.Tests.Persistence;

public sealed class SqliteSearchRepositoryTests
{
    private static readonly DateTime s_createdAt = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);
    private static readonly SupplierId s_albatross = SupplierId.Create("albatross-freight");
    private static readonly SupplierId s_bramblewood = SupplierId.Create("bramblewood-cargo");

    private static readonly SearchCriteria s_criteria = new(
        Route.Create(Location.Create("Zürich"), Location.Create("Rotterdam")),
        ShippingDateRange.Create(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8)));

    [Fact]
    [Trait("Requirement", "DB1")]
    public async Task Restores_a_running_search_with_its_responses()
    {
        await using SqliteTestDatabase database = await CreateDatabaseAsync();
        var repository = new SqliteSearchRepository(database);
        var search = Search.Start(s_criteria, [s_albatross, s_bramblewood], s_createdAt);
        SupplierResponse quote = search.RecordQuote(s_albatross, Money.Create(1234.56m, "USD"), TimeSpan.FromMilliseconds(1_234), s_createdAt.AddSeconds(1.234));

        var finished = Search.Start(s_criteria, [s_albatross], s_createdAt.AddMinutes(-1));
        Complete(finished);
        await repository.AddAsync(finished, Token);
        await repository.AddAsync(search, Token);
        IReadOnlyList<Search> running = await repository.GetRunningAsync(Token);

        Search restored = running.Should().ContainSingle().Subject;
        restored.Id.Should().Be(search.Id);
        restored.Criteria.Should().Be(s_criteria);
        restored.SelectedSuppliers.Should().Equal(s_albatross, s_bramblewood);
        restored.CreatedAt.Should().Be(s_createdAt);
        restored.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        SupplierResponse restoredQuote = restored.Responses.Should().ContainSingle().Subject;
        restoredQuote.Id.Should().Be(quote.Id);
        restoredQuote.Price.Should().Be(Money.Create(1234.56m, "USD"));
        restoredQuote.ResponseTime.Should().Be(TimeSpan.FromMilliseconds(1_234));
        restoredQuote.ReceivedAt.Should().Be(s_createdAt.AddSeconds(1.234));
        restoredQuote.ReceivedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    [Trait("Requirement", "DB2")]
    public async Task Saves_the_outcome_and_the_closing_responses_together()
    {
        await using SqliteTestDatabase database = await CreateDatabaseAsync();
        var repository = new SqliteSearchRepository(database);
        var search = Search.Start(s_criteria, [s_albatross, s_bramblewood], s_createdAt);
        await repository.AddAsync(search, Token);
        SupplierResponse failure = search.RecordFailure(s_albatross, "supplier_unavailable", "Unavailable.", TimeSpan.FromSeconds(2), s_createdAt.AddSeconds(2));
        await repository.AddResponseAsync(search.Id, failure, Token);

        IReadOnlyList<SupplierResponse> closing = search.TimeOut(s_createdAt.AddSeconds(6));
        await repository.SaveOutcomeAsync(search, closing, Token);

        await using PriceHuntDbContext db = database.CreateDbContext();
        SearchEntity stored = await db.Searches.AsNoTracking().Include(entity => entity.Responses).SingleAsync(Token);
        stored.Status.Should().Be(SearchStatus.TimedOut);
        stored.CompletedAt.Should().Be(s_createdAt.AddSeconds(6));
        stored.CompletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        stored.Responses.Should().HaveCount(2);
        SupplierResponseEntity timedOut = stored.Responses.Single(response => response.SupplierId == s_bramblewood.Value);
        timedOut.Outcome.Should().Be(ResponseOutcome.TimedOut);
        timedOut.PriceMinorUnits.Should().BeNull();
        timedOut.ResponseTimeMs.Should().Be(6_000);
        (await repository.GetRunningAsync(Token)).Should().BeEmpty();
    }

    [Fact]
    [Trait("Requirement", "DB2")]
    public async Task Stores_money_as_minor_units_and_enums_as_text()
    {
        await using SqliteTestDatabase database = await CreateDatabaseAsync();
        var repository = new SqliteSearchRepository(database);
        var search = Search.Start(s_criteria, [s_albatross], s_createdAt);
        await repository.AddAsync(search, Token);
        await repository.AddResponseAsync(search.Id, search.RecordQuote(s_albatross, Money.Create(1234.56m, "USD"), TimeSpan.FromSeconds(1), s_createdAt.AddSeconds(1)), Token);

        await using PriceHuntDbContext db = database.CreateDbContext();
        (await Scalar<long>(db, "SELECT PriceMinorUnits AS Value FROM SupplierResponses")).Should().Be(123_456);
        (await Scalar<string>(db, "SELECT Currency AS Value FROM SupplierResponses")).Should().Be("USD");
        (await Scalar<string>(db, "SELECT Outcome AS Value FROM SupplierResponses")).Should().Be("Succeeded");
        (await Scalar<string>(db, "SELECT Status AS Value FROM Searches")).Should().Be("Running");
        (await Scalar<string>(db, "SELECT OriginNormalized AS Value FROM Searches")).Should().Be("ZÜRICH");
    }

    [Fact]
    [Trait("Requirement", "DB2")]
    public async Task Allows_only_one_response_per_supplier_and_search()
    {
        await using SqliteTestDatabase database = await CreateDatabaseAsync();
        var repository = new SqliteSearchRepository(database);
        var search = Search.Start(s_criteria, [s_albatross], s_createdAt);
        await repository.AddAsync(search, Token);
        await repository.AddResponseAsync(search.Id, search.RecordQuote(s_albatross, Money.Create(1m, "USD"), TimeSpan.Zero, s_createdAt), Token);
        SupplierResponse duplicate = Search.Start(s_criteria, [s_albatross], s_createdAt)
            .RecordQuote(s_albatross, Money.Create(2m, "USD"), TimeSpan.Zero, s_createdAt);

        Func<Task> insert = () => repository.AddResponseAsync(search.Id, duplicate, Token);

        await insert.Should().ThrowAsync<DbUpdateException>();
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Task<SqliteTestDatabase> CreateDatabaseAsync() =>
        SqliteTestDatabase.CreateAsync((s_albatross.Value, "Albatross Freight"), (s_bramblewood.Value, "Bramblewood Cargo"));

    private static Task<T> Scalar<T>(PriceHuntDbContext db, string sql) => db.Database.SqlQueryRaw<T>(sql).SingleAsync(Token);

    private static void Complete(Search search)
    {
        foreach (SupplierId supplier in search.PendingSuppliers)
        {
            search.RecordQuote(supplier, Money.Create(1m, "USD"), TimeSpan.Zero, search.CreatedAt);
        }

        search.Complete(search.CreatedAt);
    }
}
