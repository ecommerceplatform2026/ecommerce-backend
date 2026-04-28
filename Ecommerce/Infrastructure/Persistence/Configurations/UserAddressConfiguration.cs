using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
    {
        public void Configure(EntityTypeBuilder<UserAddress> builder)
        {
            builder.HasKey(ua => ua.Id);
            builder.Property(ua => ua.ReceiverName).IsRequired().HasMaxLength(100);
            builder.Property(ua => ua.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(ua => ua.AddressLine).IsRequired().HasMaxLength(255);
            builder.Property(ua => ua.Ward).HasMaxLength(100);
            builder.Property(ua => ua.District).HasMaxLength(100);
            builder.Property(ua => ua.Province).HasMaxLength(100);
            builder.Property(ua => ua.IsDefault).IsRequired();

            builder.HasOne(ua => ua.User)
                   .WithMany(u => u.UserAddresses)
                   .HasForeignKey(ua => ua.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
