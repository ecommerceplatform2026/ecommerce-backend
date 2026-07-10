using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.EventType).IsRequired().HasMaxLength(500);
            builder.Property(m => m.JsonContent).IsRequired();
            builder.Property(m => m.CreatedAt).IsRequired();
            builder.Property(m => m.ProcessedAt);
            builder.Property(m => m.RetryCount).IsRequired().HasDefaultValue(0);
            builder.Property(m => m.LastError);

            builder.HasIndex(m => new { m.ProcessedAt, m.CreatedAt });
        }
    }
}
