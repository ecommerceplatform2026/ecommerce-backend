using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.PaymentLinkId).IsRequired().HasMaxLength(100);
            builder.Property(p => p.OrderCode).IsRequired();
            builder.Property(p => p.CheckoutUrl).HasMaxLength(500);
            builder.ComplexProperty(p => p.Amount, a =>
            {
                a.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
                a.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(10);
            });
            builder.Property(p => p.Status).IsRequired();

            builder.HasOne(p => p.Order)
                   .WithOne(o => o.Payment)
                   .HasForeignKey<Payment>(p => p.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
