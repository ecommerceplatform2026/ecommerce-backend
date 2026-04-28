using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.HasKey(ci => ci.Id);
            builder.Property(ci => ci.Quantity).IsRequired();

            builder.HasOne(ci => ci.User)
                   .WithMany(u => u.CartItems)
                   .HasForeignKey(ci => ci.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ci => ci.ProductVariant)
                   .WithMany(pv => pv.CartItems)
                   .HasForeignKey(ci => ci.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
