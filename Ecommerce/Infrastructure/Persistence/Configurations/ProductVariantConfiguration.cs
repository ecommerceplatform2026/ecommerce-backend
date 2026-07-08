using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.HasKey(pv => pv.Id);
            builder.Property(pv => pv.SKU)
                   .HasConversion(s => s.Value, v => new Sku(v))
                   .IsRequired()
                   .HasMaxLength(50);
            builder.Property(pv => pv.Color).HasMaxLength(50);
            builder.Property(pv => pv.Size).HasMaxLength(20);
            builder.Property(pv => pv.Stock).IsRequired().HasColumnType("bigint").IsConcurrencyToken();
            builder.Property(pv => pv.LowStockThreshold)
                   .IsRequired()
                   .HasColumnType("bigint")
                   .HasDefaultValue(5L);
            builder.Property(pv => pv.Price)
                   .HasConversion(m => m.Amount, a => new Money(a, "VND"))
                   .IsRequired()
                   .HasColumnType("bigint");

            builder.HasIndex(pv => pv.SKU)
                   .IsUnique()
                   .HasFilter("\"IsDeleted\" = false");

            builder.HasOne(pv => pv.Product)
                   .WithMany(p => p.ProductVariants)
                   .HasForeignKey(pv => pv.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
