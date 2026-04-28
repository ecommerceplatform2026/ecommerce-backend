using Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;

namespace Infrastructure.Data
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyQueryFilter(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(ModelBuilderExtensions)
                        .GetMethod(nameof(ApplyFilter), BindingFlags.NonPublic | BindingFlags.Static)
                        ?.MakeGenericMethod(entityType.ClrType);

                    method?.Invoke(null, new object[] { modelBuilder });
                }
            }
        }

        private static void ApplyFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
