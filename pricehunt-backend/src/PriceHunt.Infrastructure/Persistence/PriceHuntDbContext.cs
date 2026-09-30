using Microsoft.EntityFrameworkCore;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>The SQLite database; used through short-lived contexts from <see cref="IDbContextFactory{TContext}"/>.</summary>
internal sealed class PriceHuntDbContext(DbContextOptions<PriceHuntDbContext> options) : DbContext(options)
{
    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();

    public DbSet<SearchEntity> Searches => Set<SearchEntity>();

    public DbSet<SearchSupplierEntity> SearchSuppliers => Set<SearchSupplierEntity>();

    public DbSet<SupplierResponseEntity> SupplierResponses => Set<SupplierResponseEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PriceHuntDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }
}
