using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
    {
        public void Configure(EntityTypeBuilder<Delivery> builder)
        {
            builder.ToTable("Delivery");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.CarrierCode).IsRequired().HasMaxLength(20);
            builder.Property(d => d.TrackingCode).IsRequired().HasMaxLength(50);
            builder.Property(d => d.CarrierOrderCode).HasMaxLength(50);
            builder.Property(d => d.ToName).IsRequired().HasMaxLength(100);
            builder.Property(d => d.ToPhone).IsRequired().HasMaxLength(20);
            builder.Property(d => d.ToAddress).IsRequired().HasMaxLength(255);
            builder.Property(d => d.Province).HasMaxLength(100);
            builder.Property(d => d.District).HasMaxLength(100);
            builder.Property(d => d.Ward).HasMaxLength(100);
            builder.Property(d => d.Note).HasMaxLength(500);
            builder.Property(d => d.Status).IsRequired();

            builder.HasOne(d => d.Order)
                   .WithOne(o => o.Delivery)
                   .HasForeignKey<Delivery>(d => d.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
