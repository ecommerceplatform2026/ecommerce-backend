using Application.Interfaces.Services;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Data
{
    public class EcommerceContext : BaseDbContext
    {
        public EcommerceContext(
            DbContextOptions<EcommerceContext> options,
            ICurrentUserService currentUserService) : base(options, currentUserService.GetUserIdOrNull())
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<RecentlyViewedProduct> RecentlyViewedProducts { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<Enum>()
                .HaveConversion<string>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcommerceContext).Assembly);
            modelBuilder.ApplyQueryFilter();
        }
    }
}
