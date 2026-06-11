using Bogus;
using Domain.Entities;
using Domain.Enums;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.PerformanceTests;

public sealed class SeedDataGenerator
{
    private readonly IServiceProvider _services;
    private static readonly string[] ColorOptions = ["Red", "Blue", "Black", "White", "Green", "Yellow", "Purple", "Orange", "Pink", "Gray"];
    private static readonly string[] SizeOptions = ["S", "M", "L", "XL", "XXL"];

    public SeedDataGenerator(IServiceProvider services)
    {
        _services = services;
    }

    public async Task SeedAsync()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EcommerceContext>();

        await context.Database.MigrateAsync();

        var random = new Random(42);
        var faker = new Faker { Random = new Bogus.Randomizer(42) };

        var users = GenerateUsers(100, faker);
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var admin = User.Create("Admin", "admin@test.com", BCrypt.Net.BCrypt.HashPassword("Admin123!"));
        var regularUser = User.Create("User", "user@test.com", BCrypt.Net.BCrypt.HashPassword("User123!"));
        context.Users.AddRange(admin, regularUser);
        await context.SaveChangesAsync();

        var categories = GenerateCategories(50, faker);
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        var allUsers = await context.Users.ToListAsync();
        var allCategories = await context.Categories.ToListAsync();

        var (products, variants) = GenerateProductsAndVariants(allCategories, 10000, faker, random);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        foreach (var batch in variants.Chunk(1000))
        {
            context.ProductVariants.AddRange(batch);
            await context.SaveChangesAsync();
        }

        var allProducts = await context.Products.ToListAsync();
        var allVariants = await context.ProductVariants.ToListAsync();
        var regularUserId = regularUser.Id;

        var addresses = new List<UserAddress>();
        foreach (var user in allUsers.Take(100))
        {
            var addrCount = random.Next(1, 3);
            for (int i = 0; i < addrCount; i++)
            {
                addresses.Add(UserAddress.Create(
                    user.Id, faker.Name.FullName(), faker.Phone.PhoneNumber(),
                    faker.Address.StreetAddress(), faker.Address.City(),
                    faker.Address.County(), faker.Address.State(), i == 0));
            }
        }
        context.UserAddresses.AddRange(addresses);

        var testAddress = UserAddress.Create(regularUserId, "Test User", "0900000000",
            "123 Test Street", "Ward 1", "District 1", "HCMC", true);
        context.UserAddresses.Add(testAddress);
        await context.SaveChangesAsync();

        var statusWeights = new (OrderStatus, double)[] {
            (OrderStatus.Pending, 0.20),
            (OrderStatus.Confirmed, 0.10),
            (OrderStatus.Processing, 0.10),
            (OrderStatus.Shipping, 0.10),
            (OrderStatus.Delivered, 0.10),
            (OrderStatus.Completed, 0.25),
            (OrderStatus.Cancelled, 0.10),
            (OrderStatus.Returned, 0.05)
        };

        var paymentMethods = new[] { PaymentMethod.COD, PaymentMethod.MoMo, PaymentMethod.ZaloPay, PaymentMethod.VNPay };

        var testPendingOrder = Domain.Entities.Order.Create(regularUserId, random.Next(100000, 999999), PaymentMethod.COD);
        testPendingOrder.AddItem(allVariants[0].Id, 1, allVariants[0].Price, "Test Product");
        context.Orders.Add(testPendingOrder);

        var testCompletedOrder = Domain.Entities.Order.Create(regularUserId, random.Next(100000, 999999), PaymentMethod.COD);
        testCompletedOrder.AddItem(allVariants[0].Id, 1, allVariants[0].Price, "Test Product");
        testCompletedOrder.MarkAsConfirmed();
        testCompletedOrder.MarkAsProcessing();
        testCompletedOrder.MarkAsShipping();
        testCompletedOrder.MarkAsDelivered();
        testCompletedOrder.MarkAsCompleted();
        context.Orders.Add(testCompletedOrder);

        var orders = new List<Domain.Entities.Order>();
        for (int i = 0; i < 1000; i++)
        {
            var user = allUsers[random.Next(allUsers.Count)];
            var method = paymentMethods[random.Next(paymentMethods.Length)];
            var order = Domain.Entities.Order.Create(user.Id, 100000 + i + 1000, method);
            order.ApplyDiscount(0);

            int itemCount = random.Next(2, 5);
            var usedVariants = new HashSet<Guid>();
            for (int j = 0; j < itemCount; j++)
            {
                var v = allVariants[random.Next(allVariants.Count)];
                if (usedVariants.Add(v.Id))
                {
                    var qty = random.Next(1, 4);
                    order.AddItem(v.Id, qty, v.Price, $"Item-{v.Id:N}");
                }
            }

            double roll = random.NextDouble();
            double cumulative = 0;
            OrderStatus? targetStatus = null;
            foreach (var (status, weight) in statusWeights)
            {
                cumulative += weight;
                if (roll <= cumulative)
                {
                    targetStatus = status;
                    break;
                }
            }

            if (targetStatus.HasValue)
            {
                try
                {
                    switch (targetStatus.Value)
                    {
                        case OrderStatus.Pending: break;
                        case OrderStatus.Confirmed: order.MarkAsConfirmed(); break;
                        case OrderStatus.Processing: order.MarkAsConfirmed(); order.MarkAsProcessing(); break;
                        case OrderStatus.Shipping: order.MarkAsConfirmed(); order.MarkAsProcessing(); order.MarkAsShipping(); break;
                        case OrderStatus.Delivered: order.MarkAsConfirmed(); order.MarkAsProcessing(); order.MarkAsShipping(); order.MarkAsDelivered(); break;
                        case OrderStatus.Completed: order.MarkAsConfirmed(); order.MarkAsProcessing(); order.MarkAsShipping(); order.MarkAsDelivered(); order.MarkAsCompleted(); break;
                        case OrderStatus.Cancelled: break;
                        case OrderStatus.Returned: break;
                    }
                }
                catch { }
            }

            orders.Add(order);
        }

        foreach (var batch in orders.Chunk(100))
        {
            context.Orders.AddRange(batch);
            await context.SaveChangesAsync();
        }

        var allOrders = await context.Orders.Include(o => o.OrderItems).ToListAsync();
        var completedOrders = allOrders.Where(o => o.Status == OrderStatus.Completed).ToList();

        var reviews = new List<Review>();
        for (int i = 0; i < 5000 && completedOrders.Count > 0; i++)
        {
            var order = completedOrders[random.Next(completedOrders.Count)];
            var product = allProducts[random.Next(allProducts.Count)];
            reviews.Add(Review.Create(
                order.UserId, product.Id, order.Id,
                random.Next(3, 6),
                faker.Commerce.ProductName(),
                faker.Lorem.Sentence()));
        }
        foreach (var batch in reviews.Chunk(500))
        {
            context.Set<Review>().AddRange(batch);
            await context.SaveChangesAsync();
        }

        var testReview = Review.Create(regularUserId, allProducts[0].Id, testCompletedOrder.Id, 5, "Great product", "Fast shipping and great quality");
        context.Set<Review>().Add(testReview);
        await context.SaveChangesAsync();

        var loyaltyAccounts = new List<LoyaltyAccount>();
        foreach (var user in allUsers)
        {
            var acct = LoyaltyAccount.Create(user.Id);
            acct.AddAvailablePoints(random.Next(100, 10000));
            loyaltyAccounts.Add(acct);
        }

        var testLoyalty = LoyaltyAccount.Create(regularUserId);
        testLoyalty.AddAvailablePoints(5000);
        loyaltyAccounts.Add(testLoyalty);

        context.LoyaltyAccounts.AddRange(loyaltyAccounts);
        await context.SaveChangesAsync();
    }

    private static List<User> GenerateUsers(int count, Faker faker)
    {
        var users = new List<User>();
        for (int i = 0; i < count; i++)
        {
            var name = faker.Name.FullName();
            var email = faker.Internet.Email();
            users.Add(User.Create(name, email, BCrypt.Net.BCrypt.HashPassword("Test123!")));
        }
        return users;
    }

    private static List<Category> GenerateCategories(int count, Faker faker)
    {
        var names = new HashSet<string>();
        var cats = new List<Category>();
        for (int i = 0; i < count; i++)
        {
            var name = faker.Commerce.Categories(1)[0];
            while (!names.Add(name))
                name = faker.Commerce.Categories(1)[0];
            cats.Add(Category.Create(name));
        }
        return cats;
    }

    private static (List<Product> products, List<ProductVariant> variants) GenerateProductsAndVariants(
        List<Category> categories, int productCount, Faker faker, Random random)
    {
        var products = new List<Product>();
        var variants = new List<ProductVariant>();

        for (int i = 0; i < productCount; i++)
        {
            var category = categories[random.Next(categories.Count)];
            var name = faker.Commerce.ProductName();
            var desc = faker.Lorem.Sentence();
            var material = faker.PickRandom("Cotton", "Polyester", "Leather", "Wool", "Linen", "Silk", "Denim", "Nylon");
            var price = new Money(random.Next(50000, 5000000));
            var status = faker.PickRandom(ProductStatus.Active, ProductStatus.Active, ProductStatus.Active, ProductStatus.Inactive);

            var product = Product.Create(category.Id, name, desc, material, price, status);
            products.Add(product);

            int variantCount = random.Next(2, 5);
            for (int j = 0; j < variantCount; j++)
            {
                var color = ColorOptions[random.Next(ColorOptions.Length)];
                var size = SizeOptions[random.Next(SizeOptions.Length)];
                var sku = new Sku($"SKU-{i:D5}-{color[..2]}{size}");
                var vPrice = new Money(price.Amount + random.Next(-10000, 10001));
                var stock = random.Next(10, 500);
                var lowThreshold = random.Next(5, 20);

                var variant = ProductVariant.Create(product.Id, sku, color, size, stock, vPrice);
                variants.Add(variant);
            }
        }

        return (products, variants);
    }
}
