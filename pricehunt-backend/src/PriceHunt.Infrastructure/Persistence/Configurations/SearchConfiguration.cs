using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

internal sealed class SearchConfiguration : IEntityTypeConfiguration<SearchEntity>
{
    public void Configure(EntityTypeBuilder<SearchEntity> builder)
    {
        builder.ToTable("Searches");
        builder.HasKey(search => search.Id);
        builder.Property(search => search.Id).ValueGeneratedNever();
        builder.Property(search => search.Origin).HasMaxLength(Location.MaxLength);
        builder.Property(search => search.OriginNormalized).HasMaxLength(Location.MaxLength);
        builder.Property(search => search.Destination).HasMaxLength(Location.MaxLength);
        builder.Property(search => search.DestinationNormalized).HasMaxLength(Location.MaxLength);
        builder.Property(search => search.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasMany(search => search.Suppliers).WithOne().HasForeignKey(selection => selection.SearchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(search => search.Responses).WithOne(response => response.Search).HasForeignKey(response => response.SearchId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(search => search.Status);
        builder.HasIndex(search => search.OriginNormalized);
        builder.HasIndex(search => search.DestinationNormalized);
    }
}
