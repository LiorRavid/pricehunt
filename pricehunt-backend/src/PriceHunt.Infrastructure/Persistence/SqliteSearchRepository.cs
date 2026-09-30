using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PriceHunt.Application.Searches;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// Stores searches through a short-lived context per operation, so no context is shared between
/// concurrent searches and nothing depends on the request's service scope.
/// </summary>
internal sealed class SqliteSearchRepository(IDbContextFactory<PriceHuntDbContext> contextFactory) : ISearchRepository
{
    public async Task AddAsync(Search search, CancellationToken cancellationToken)
    {
        await using PriceHuntDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.Searches.Add(PersistenceMapping.ToEntity(search));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddResponseAsync(Guid searchId, SupplierResponse response, CancellationToken cancellationToken)
    {
        await using PriceHuntDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.SupplierResponses.Add(PersistenceMapping.ToEntity(searchId, response));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveOutcomeAsync(Search search, IReadOnlyCollection<SupplierResponse> closingResponses, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);
        await using PriceHuntDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await db.Searches
            .Where(entity => entity.Id == search.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(entity => entity.Status, search.Status)
                    .SetProperty(entity => entity.CompletedAt, search.CompletedAt),
                cancellationToken)
            .ConfigureAwait(false);
        db.SupplierResponses.AddRange(closingResponses.Select(response => PersistenceMapping.ToEntity(search.Id, response)));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Search>> GetRunningAsync(CancellationToken cancellationToken)
    {
        await using PriceHuntDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        List<SearchEntity> running = await db.Searches
            .AsNoTracking()
            .Include(search => search.Suppliers)
            .Include(search => search.Responses)
            .AsSplitQuery()
            .Where(search => search.Status == SearchStatus.Running)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. running.Select(PersistenceMapping.ToDomain)];
    }
}
