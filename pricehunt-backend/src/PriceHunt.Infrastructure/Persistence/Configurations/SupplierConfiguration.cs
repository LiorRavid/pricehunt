using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<SupplierEntity>
{
    public void Configure(EntityTypeBuilder<SupplierEntity> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(supplier => supplier.Id);
        builder.Property(supplier => supplier.Id).HasMaxLength(SupplierId.MaxLength);
        builder.Property(supplier => supplier.Name).HasMaxLength(100);
        builder.HasIndex(supplier => supplier.Name);
    }
}
