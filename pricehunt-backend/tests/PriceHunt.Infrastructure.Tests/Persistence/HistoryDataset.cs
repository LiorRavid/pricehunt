using PriceHunt.Domain;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Infrastructure.Tests.Persistence;

/// <summary>
/// Three searches with known timestamps, prices, response times and outcomes, shared by the
/// history query tests (they only read). Responses are labelled "search-supplier".
/// </summary>
public sealed class HistoryDataset : IAsyncLifetime
{
    public static readonly DateTime Start = new(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);

    public static readonly SupplierId Alpha = SupplierId.Create("alpha");
    public static readonly SupplierId Bravo = SupplierId.Create("bravo");
    public static readonly SupplierId Charlie = SupplierId.Create("charlie");

    private readonly Dictionary<Guid, string> _labels = [];
    private SqliteTestDatabase? _database;

    internal SqliteTestDatabase Database => _database ?? throw new InvalidOperationException("The dataset isn't initialized.");

    public string LabelOf(Guid responseId) => _labels[responseId];

    public async ValueTask InitializeAsync()
    {
        _database = await SqliteTestDatabase.CreateAsync(("alpha", "Alpha Lines"), ("bravo", "Bravo Cargo"), ("charlie", "Charlie Freight"));
        var repository = new SqliteSearchRepository(_database);

        var first = Search.Start(Criteria("Haifa", "Rotterdam"), [Alpha, Bravo, Charlie], Start);
        Label("s1-alpha", first.RecordQuote(Alpha, Usd(1500m), Milliseconds(1_200), Start.AddMilliseconds(1_200)));
        Label("s1-charlie", first.RecordFailure(Charlie, "supplier_unavailable", "Unavailable.", Milliseconds(2_000), Start.AddMilliseconds(2_000)));
        Label("s1-bravo", first.RecordQuote(Bravo, Usd(900.50m), Milliseconds(3_000), Start.AddMilliseconds(3_000)));
        first.Complete(Start.AddSeconds(3));
        await repository.AddAsync(first, CancellationToken.None);

        DateTime secondStart = Start.AddDays(1);
        var second = Search.Start(Criteria("zürich", "Haifa"), [Alpha, Bravo, Charlie], secondStart);
        Label("s2-alpha", second.RecordQuote(Alpha, Usd(1100m), Milliseconds(500), secondStart.AddMilliseconds(500)));
        Label("s2-charlie", second.RecordQuote(Charlie, Usd(1100m), Milliseconds(4_000), secondStart.AddMilliseconds(4_000)));
        Label("s2-bravo", second.TimeOut(secondStart.AddSeconds(6)).Single());
        await repository.AddAsync(second, CancellationToken.None);

        DateTime thirdStart = Start.AddDays(2);
        var third = Search.Start(Criteria("Rotterdam", "Antwerp"), [Alpha], thirdStart);
        Label("s3-alpha", third.RecordQuote(Alpha, Usd(2000m), Milliseconds(800), thirdStart.AddMilliseconds(800)));
        third.Complete(thirdStart.AddSeconds(1));
        await repository.AddAsync(third, CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private static SearchCriteria Criteria(string origin, string destination) => new(
        Route.Create(Location.Create(origin), Location.Create(destination)),
        ShippingDateRange.Create(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8)));

    private static Money Usd(decimal amount) => Money.Create(amount, "USD");

    private static TimeSpan Milliseconds(int value) => TimeSpan.FromMilliseconds(value);

    private void Label(string label, SupplierResponse response) => _labels[response.Id] = label;
}
