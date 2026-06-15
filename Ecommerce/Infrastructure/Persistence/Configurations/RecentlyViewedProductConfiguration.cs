using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class RecentlyViewedProductConfiguration : IEntityTypeConfiguration<RecentlyViewedProduct>
    {
        public void Configure(EntityTypeBuilder<RecentlyViewedProduct> builder)
        {
            builder.HasKey(r => r.Id);

            builder.HasIndex(r => new { r.UserId, r.ProductId })
                   .IsUnique();

            builder.HasIndex(r => r.CreatedAt);

            builder.HasOne(r => r.User)
                   .WithMany(u => u.RecentlyViewedProducts)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Product)
                   .WithMany()
                   .HasForeignKey(r => r.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
