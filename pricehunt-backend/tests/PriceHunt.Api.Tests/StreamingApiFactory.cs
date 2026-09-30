using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using PriceHunt.Api.Tests.Fakes;
using PriceHunt.Application.Searches;
using PriceHunt.Application.Suppliers;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Api.Tests;

/// <summary>
/// The API with three hand-answered suppliers, a fake clock, and a repository that signals when a
/// search is finalised. Create one per test: its suppliers answer once.
/// </summary>
public sealed class StreamingApiFactory : PriceHuntApiFactory
{
    private ObservedSearchRepository? _repository;

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.Zero));

    internal ControllableSupplier Aurora { get; } = new("aurora", "Aurora Freight");

    internal ControllableSupplier Bluefin { get; } = new("bluefin", "Bluefin Cargo");

    internal ControllableSupplier Copper { get; } = new("copper", "Copper Lines");

    internal ObservedSearchRepository Repository => _repository ?? throw new InvalidOperationException("The server hasn't started yet.");

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(Time);

        services.RemoveAll<ISupplierCatalog>();
        services.AddSingleton<ISupplierCatalog>(new Catalog([Aurora, Bluefin, Copper]));

        services.RemoveAll<ISearchRepository>();
        services.AddSingleton<SqliteSearchRepository>();
        services.AddSingleton<ISearchRepository>(provider =>
            _repository = new ObservedSearchRepository(provider.GetRequiredService<SqliteSearchRepository>()));
    }

    private sealed class Catalog(IReadOnlyList<IShippingSupplier> suppliers) : ISupplierCatalog
    {
        public IReadOnlyList<IShippingSupplier> Suppliers { get; } = suppliers;
    }
}
