using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

internal sealed class SearchSupplierConfiguration : IEntityTypeConfiguration<SearchSupplierEntity>
{
    public void Configure(EntityTypeBuilder<SearchSupplierEntity> builder)
    {
        builder.ToTable("SearchSuppliers");
        builder.HasKey(selection => new { selection.SearchId, selection.SupplierId });
        builder.Property(selection => selection.SupplierId).HasMaxLength(SupplierId.MaxLength);
        builder.HasOne<SupplierEntity>().WithMany().HasForeignKey(selection => selection.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
