using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
    {
        public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
        {
            builder.HasKey(a => a.Id);
            builder.HasIndex(a => a.UserId).IsUnique();
            builder.Property(a => a.AvailablePoints).IsRequired();
            builder.Property(a => a.PendingPoints).IsRequired();

            builder.HasOne(a => a.User)
                   .WithOne(u => u.LoyaltyAccount)
                   .HasForeignKey<LoyaltyAccount>(a => a.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
