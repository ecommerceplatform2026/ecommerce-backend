using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(oi => oi.Id);
            builder.Property(oi => oi.Quantity).IsRequired();
            builder.Property(oi => oi.Price)
                   .HasConversion(m => m.Amount, a => new Money(a, "VND"))
                   .IsRequired();
            builder.Property(oi => oi.ProductSnapshot).IsRequired();

            builder.HasOne(oi => oi.Order)
                   .WithMany(o => o.OrderItems)
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(oi => oi.ProductVariant)
                   .WithMany(pv => pv.OrderItems)
                   .HasForeignKey(oi => oi.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
