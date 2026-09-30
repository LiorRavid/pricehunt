using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without starting the API.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PriceHuntDbContext>
{
    public PriceHuntDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PriceHuntDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
