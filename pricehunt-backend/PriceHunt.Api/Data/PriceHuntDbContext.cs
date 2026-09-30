using Microsoft.EntityFrameworkCore;

namespace PriceHunt.Api.Data;

public class PriceHuntDbContext(DbContextOptions<PriceHuntDbContext> options) : DbContext(options)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(supplier =>
        {
            supplier.Property(s => s.Name).HasMaxLength(100);

            // Sample rows for the end-to-end smoke test (GET /api/suppliers); replace freely.
            supplier.HasData(
                new Supplier { Id = 1, Name = "Supplier A" },
                new Supplier { Id = 2, Name = "Supplier B" },
                new Supplier { Id = 3, Name = "Supplier C" },
                new Supplier { Id = 4, Name = "Supplier D" },
                new Supplier { Id = 5, Name = "Supplier E" },
                new Supplier { Id = 6, Name = "Supplier F" },
                new Supplier { Id = 7, Name = "Supplier G" });
        });
    }
}
