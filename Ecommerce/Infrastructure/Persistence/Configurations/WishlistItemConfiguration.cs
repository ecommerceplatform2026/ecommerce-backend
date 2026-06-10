using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
    {
        public void Configure(EntityTypeBuilder<WishlistItem> builder)
        {
            builder.HasKey(w => w.Id);

            builder.HasIndex(w => new { w.UserId, w.ProductVariantId })
                   .IsUnique();

            builder.HasOne(w => w.User)
                   .WithMany(u => u.WishlistItems)
                   .HasForeignKey(w => w.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(w => w.ProductVariant)
                   .WithMany(pv => pv.WishlistItems)
                   .HasForeignKey(w => w.ProductVariantId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
