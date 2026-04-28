using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Data
{
    public abstract class BaseDbContext(DbContextOptions options, string? currentUserId = null) : DbContext(options)
    {
        private readonly string _currentUserId = currentUserId ?? "system";

        private string NormalizeUserId()
        {
            if (Guid.TryParse(_currentUserId, out var guid))
            {
                return guid.ToString();
            }
            return "system";
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var userId = NormalizeUserId();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.SetCreated(userId);
                        break;

                    case EntityState.Modified:
                        if (!entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified &&
                            !entry.Property(nameof(BaseEntity.UpdatedBy)).IsModified)
                        {
                            entry.Entity.SetUpdated(userId);
                        }
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.SetDeleted(userId);
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
