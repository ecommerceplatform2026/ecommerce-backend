using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
    {
        public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
        {
            builder.HasKey(t => t.Id);
            builder.HasIndex(t => new { t.OrderId, t.Type })
                   .IsUnique()
                   .HasFilter("\"OrderId\" IS NOT NULL AND \"Type\" = 'Earn'");
            builder.Property(t => t.Points).IsRequired();
            builder.Property(t => t.Type).IsRequired();
            builder.Property(t => t.Status).IsRequired();
            builder.Property(t => t.Description).HasMaxLength(255);

            builder.HasOne(t => t.LoyaltyAccount)
                   .WithMany(a => a.Transactions)
                   .HasForeignKey(t => t.LoyaltyAccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Order)
                   .WithMany(o => o.LoyaltyTransactions)
                   .HasForeignKey(t => t.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
