using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence.Configurations;

internal sealed class SupplierResponseConfiguration : IEntityTypeConfiguration<SupplierResponseEntity>
{
    public void Configure(EntityTypeBuilder<SupplierResponseEntity> builder)
    {
        builder.ToTable("SupplierResponses");
        builder.HasKey(response => response.Id);
        builder.Property(response => response.Id).ValueGeneratedNever();
        builder.Property(response => response.SupplierId).HasMaxLength(SupplierId.MaxLength);
        builder.Property(response => response.Outcome).HasConversion<string>().HasMaxLength(16);
        builder.Property(response => response.Currency).HasMaxLength(3);
        builder.Property(response => response.ErrorCode).HasMaxLength(64);
        builder.Property(response => response.ErrorMessage).HasMaxLength(500);

        builder.HasOne(response => response.Supplier).WithMany().HasForeignKey(response => response.SupplierId).OnDelete(DeleteBehavior.Restrict);

        // One outcome per selected supplier per search.
        builder.HasIndex(response => new { response.SearchId, response.SupplierId }).IsUnique();

        // Every history filter and sort column.
        builder.HasIndex(response => response.ReceivedAt);
        builder.HasIndex(response => new { response.SupplierId, response.ReceivedAt });
        builder.HasIndex(response => new { response.Outcome, response.ReceivedAt });
        builder.HasIndex(response => response.PriceMinorUnits);
        builder.HasIndex(response => response.ResponseTimeMs);
    }
}
