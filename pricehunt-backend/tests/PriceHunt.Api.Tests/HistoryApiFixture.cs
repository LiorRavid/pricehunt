using Microsoft.Extensions.DependencyInjection;
using PriceHunt.Application.Searches;
using PriceHunt.Domain;

namespace PriceHunt.Api.Tests;

/// <summary>The API with its real suppliers and three stored searches, for the history endpoint tests.</summary>
public sealed class HistoryApiFixture : PriceHuntApiFactory, IAsyncLifetime
{
    public static readonly DateTime Start = new(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc);

    public async ValueTask InitializeAsync()
    {
        ISearchRepository repository = Services.GetRequiredService<ISearchRepository>();
        var albatross = SupplierId.Create("albatross-freight");
        var bramblewood = SupplierId.Create("bramblewood-cargo");
        var driftwood = SupplierId.Create("driftwood-shipping");

        var first = Search.Start(Criteria("Haifa", "Rotterdam"), [albatross, bramblewood, driftwood], Start);
        first.RecordQuote(albatross, Money.Create(1500m, "USD"), TimeSpan.FromMilliseconds(1_200), Start.AddMilliseconds(1_200));
        first.RecordQuote(bramblewood, Money.Create(990.25m, "USD"), TimeSpan.FromMilliseconds(2_500), Start.AddMilliseconds(2_500));
        first.RecordFailure(driftwood, "supplier_unavailable", "Driftwood Shipping is temporarily unavailable.", TimeSpan.FromSeconds(3), Start.AddSeconds(3));
        first.Complete(Start.AddSeconds(3));
        await repository.AddAsync(first, CancellationToken.None);

        DateTime secondStart = Start.AddDays(2);
        var second = Search.Start(Criteria("Antwerp", "Haifa"), [albatross], secondStart);
        second.RecordQuote(albatross, Money.Create(1200m, "USD"), TimeSpan.FromMilliseconds(700), secondStart.AddMilliseconds(700));
        second.Complete(secondStart.AddSeconds(1));
        await repository.AddAsync(second, CancellationToken.None);
    }

    private static SearchCriteria Criteria(string origin, string destination) => new(
        Route.Create(Location.Create(origin), Location.Create(destination)),
        ShippingDateRange.Create(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8)));
}
