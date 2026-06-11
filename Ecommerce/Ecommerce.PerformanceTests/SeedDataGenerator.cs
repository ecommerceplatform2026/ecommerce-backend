#pragma warning disable EF1002

using Bogus;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Ecommerce.PerformanceTests;

public sealed record SeedResult(
    Guid ProductId,
    Guid VariantId,
    Guid AddressId,
    Guid PendingOrderId,
    int PendingOrderCode,
    Guid CompletedOrderId
);

public sealed class SeedDataGenerator
{
    private readonly IServiceProvider _services;

    public SeedDataGenerator(IServiceProvider services) => _services = services;

    public async Task<SeedResult> SeedAsync()
    {
        using var scope = _services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
        await ctx.Database.MigrateAsync();

        var rand = new Random(42);
        var faker = new Faker { Random = new Bogus.Randomizer(42) };
        var now = DateTime.UtcNow;

        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", workFactor: 4);
        var userHash = BCrypt.Net.BCrypt.HashPassword("User123!", workFactor: 4);
        var testHash = BCrypt.Net.BCrypt.HashPassword("Test123!", workFactor: 4);

        // -- Users (102) --
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var fakeUserIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var allUserIds = new[] { adminId, userId }.Concat(fakeUserIds).ToArray();

        await ctx.Database.ExecuteSqlRawAsync(
            $@"INSERT INTO ""Users"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""FullName"",""Username"",""Email"",""PhoneNumber"",""PasswordHash"",""AvatarUrl"",""DateOfBirth"",""Role"",""Status"",""EmailConfirmed"")
              VALUES ({{{0}}},{{{1}}},NULL,NULL,NULL,false,NULL,NULL,'Admin',NULL,'admin@test.com',NULL,'{adminHash}',NULL,NULL,'Admin','Active',true)",
            adminId, now);
        await ctx.Database.ExecuteSqlRawAsync(
            $@"INSERT INTO ""Users"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""FullName"",""Username"",""Email"",""PhoneNumber"",""PasswordHash"",""AvatarUrl"",""DateOfBirth"",""Role"",""Status"",""EmailConfirmed"")
              VALUES ({{{0}}},{{{1}}},NULL,NULL,NULL,false,NULL,NULL,'User',NULL,'user@test.com',NULL,'{userHash}',NULL,NULL,'User','Active',true)",
            userId, now);

        var addrUserIds = new List<Guid> { userId };
        var orderUserIds = new List<Guid> { userId };
        var reviewUserIds = new List<Guid> { userId };
        var loyaltyUserIds = new List<Guid> { userId };
        var cartUserIds = new List<Guid> { userId };
        var wishlistUserIds = new List<Guid> { userId };

        for (int i = 0; i < fakeUserIds.Count; i++)
        {
            var id = fakeUserIds[i];
            var name = faker.Name.FullName().Replace("'", "''");
            var email = faker.Internet.Email().Replace("'", "''");
            await ctx.Database.ExecuteSqlRawAsync(
                $@"INSERT INTO ""Users"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""FullName"",""Username"",""Email"",""PhoneNumber"",""PasswordHash"",""AvatarUrl"",""DateOfBirth"",""Role"",""Status"",""EmailConfirmed"")
                  VALUES ({{{0}}},{{{1}}},NULL,NULL,NULL,false,NULL,NULL,'{name}',NULL,'{email}',NULL,'{testHash}',NULL,NULL,'User','Active',true)",
                id, now.AddDays(-i));

            if (i < 90) addrUserIds.Add(id);
            if (i < 80) orderUserIds.Add(id);
            if (i < 60) reviewUserIds.Add(id);
            if (i < 25) loyaltyUserIds.Add(id);
            if (i < 40) cartUserIds.Add(id);
            if (i < 50) wishlistUserIds.Add(id);
        }

        // -- Categories (50) --
        var catNames = new[] {
            "T-Shirts","Pants","Jeans","Shorts","Jackets","Coats","Suits","Dresses","Skirts","Blouses",
            "Sweaters","Cardigans","Hoodies","Vests","Tanks","Polos","Button-Downs","Chinos","Joggers","Cargos",
            "Leather Jackets","Denim Jackets","Bomber Jackets","Windbreakers","Raincoats","Blazers","Sport Coats","Tuxedos","Waistcoats","Overcoats",
            "Parkas","Fleeces","Track Pants","Sweatpants","Bermuda Shorts","Cargo Shorts","Denim Shorts","Linen Pants","Formal Pants","Cropped Pants",
            "Wide Leg Pants","Palazzo Pants","Culottes","Jumpsuits","Rompers","Bodycon Dresses","Maxi Dresses","Midi Dresses","Mini Dresses","A-Line Skirts"
        };
        var catIds = catNames.Select(_ => Guid.NewGuid()).ToList();
        var catBuilder = new StringBuilder();
        catBuilder.Append($@"INSERT INTO ""Categories"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""Name"",""Status"") VALUES ");
        for (int i = 0; i < catIds.Count; i++)
        {
            if (i > 0) catBuilder.Append(',');
            catBuilder.Append($"({{{i * 2}}},{{{i * 2 + 1}}},NULL,NULL,NULL,false,NULL,NULL,'{catNames[i].Replace("'", "''")}','Active')");
        }
        var catParams = catIds.SelectMany((id, i) => new object[] { id, now.AddMinutes(i) }).ToArray();
        await ctx.Database.ExecuteSqlRawAsync(catBuilder.ToString(), catParams);

        // -- Products (10k = 200 per category) --
        var prodIds = Enumerable.Range(0, 10000).Select(_ => Guid.NewGuid()).ToList();
        var prodBuilder = new StringBuilder();
        prodBuilder.Append($@"INSERT INTO ""Products"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""CategoryId"",""Name"",""Description"",""Material"",""BasePrice"",""Status"") VALUES ");
        var prodParams = new List<object>();
        for (int i = 0; i < prodIds.Count; i++)
        {
            if (i > 0) prodBuilder.Append(',');
            var catId = catIds[i / 200];
            var price = rand.Next(50000, 5000000);
            var name = faker.Commerce.ProductName().Replace("'", "''");
            var desc = faker.Lorem.Sentence().Replace("'", "''");
            var materials = new[] { "Cotton", "Polyester", "Wool", "Silk", "Linen", "Denim", "Leather", "Nylon", "Rayon", "Spandex" };
            var material = materials[rand.Next(materials.Length)];
            var p = prodParams.Count;
            prodParams.Add(prodIds[i]); prodParams.Add(now.AddMinutes(-i)); prodParams.Add(catId);
            prodBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},'{name}','{desc}','{material}',{price},'Active')");
            prodParams.AddRange(new object[] { });
        }
        // Execute in batches of 1000
        for (int batch = 0; batch < prodIds.Count; batch += 1000)
        {
            var batchBuilder = new StringBuilder();
            batchBuilder.Append($@"INSERT INTO ""Products"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""CategoryId"",""Name"",""Description"",""Material"",""BasePrice"",""Status"") VALUES ");
            var batchParams = new List<object>();
            for (int j = 0; j < 1000 && batch + j < prodIds.Count; j++)
            {
                var i = batch + j;
                if (j > 0) batchBuilder.Append(',');
                var catId = catIds[i / 200];
                var price = rand.Next(50000, 5000000);
                var name = faker.Commerce.ProductName().Replace("'", "''");
                var desc = faker.Lorem.Sentence().Replace("'", "''");
                var materials = new[] { "Cotton", "Polyester", "Wool", "Silk", "Linen", "Denim", "Leather", "Nylon", "Rayon", "Spandex" };
                var material = materials[rand.Next(materials.Length)];
                var p = batchParams.Count;
                batchParams.Add(prodIds[i]); batchParams.Add(now.AddMinutes(-i)); batchParams.Add(catId);
                batchBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},'{name}','{desc}','{material}',{price},'Active')");
            }
            await ctx.Database.ExecuteSqlRawAsync(batchBuilder.ToString(), batchParams.ToArray());
        }

        // -- Variants (30k = 3 per product) --
        var colors = new[] { "Red", "Blue", "Black", "White", "Green", "Yellow", "Purple", "Orange", "Pink", "Gray" };
        var sizes = new[] { "XS", "S", "M", "L", "XL", "XXL" };
        var varIds = new List<Guid>();
        for (int b = 0; b < prodIds.Count; b += 333)
        {
            var count = Math.Min(333, prodIds.Count - b);
            varIds.Clear();
            for (int j = 0; j < count; j++)
            {
                varIds.Add(Guid.NewGuid()); varIds.Add(Guid.NewGuid()); varIds.Add(Guid.NewGuid());
            }
            var vBatchBuilder = new StringBuilder();
            vBatchBuilder.Append($@"INSERT INTO ""ProductVariants"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""ProductId"",""SKU"",""Color"",""Size"",""Stock"",""LowStockThreshold"",""Price"") VALUES ");
            var vParams = new List<object>();
            var index = 0;
            for (int j = 0; j < count; j++)
            {
                var prodIdx = b + j;
                for (int v = 0; v < 3; v++)
                {
                    if (index > 0) vBatchBuilder.Append(',');
                    var sku = $"SKU-{prodIdx:D5}-{v}";
                    var color = colors[rand.Next(colors.Length)];
                    var size = sizes[rand.Next(sizes.Length)];
                    var stock = rand.Next(0, 200);
                    var price = rand.Next(40000, 5000000);
                    var p = vParams.Count;
                    vParams.Add(varIds[j * 3 + v]); vParams.Add(now); vParams.Add(prodIds[prodIdx]);
                    vBatchBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},'{sku}','{color}','{size}',{stock},5,{price})");
                    index++;
                }
            }
            await ctx.Database.ExecuteSqlRawAsync(vBatchBuilder.ToString(), vParams.ToArray());
        }
        // Get reference variants
        var refVarIds = new List<Guid>();
        for (int i = 0; i < Math.Min(1000, prodIds.Count); i++)
        {
            refVarIds.Add(Guid.NewGuid()); refVarIds.Add(Guid.NewGuid()); refVarIds.Add(Guid.NewGuid());
        }
        // Re-insert variants for first 1000 products (reference variants)
        // Actually let's just use the regular variant IDs for first 1000 products
        var varIdsFirst3 = new List<Guid>();
        for (int i = 0; i < 1000; i++)
        {
            varIdsFirst3.Add(Guid.NewGuid()); varIdsFirst3.Add(Guid.NewGuid()); varIdsFirst3.Add(Guid.NewGuid());
        }
        {
            var vBatchBuilder = new StringBuilder();
            vBatchBuilder.Append($@"INSERT INTO ""ProductVariants"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""ProductId"",""SKU"",""Color"",""Size"",""Stock"",""LowStockThreshold"",""Price"") VALUES ");
            var vParams = new List<object>();
            for (int i = 0; i < 1000; i++)
            {
                for (int v = 0; v < 3; v++)
                {
                    if (i * 3 + v > 0) vBatchBuilder.Append(',');
                    var sku = $"SKU-REF-{i:D5}-{v}";
                    var color = colors[rand.Next(colors.Length)];
                    var size = sizes[rand.Next(sizes.Length)];
                    var stock = rand.Next(0, 200);
                    var price = rand.Next(40000, 5000000);
                    var p = vParams.Count;
                    vParams.Add(varIdsFirst3[i * 3 + v]); vParams.Add(now); vParams.Add(prodIds[i]);
                    vBatchBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},'{sku}','{color}','{size}',{stock},5,{price})");
                }
            }
            await ctx.Database.ExecuteSqlRawAsync(vBatchBuilder.ToString(), vParams.ToArray());
        }

        // -- Product Images (10k = 1 per product) --
        for (int b = 0; b < prodIds.Count; b += 1000)
        {
            var count = Math.Min(1000, prodIds.Count - b);
            var piBuilder = new StringBuilder();
            piBuilder.Append($@"INSERT INTO ""ProductImages"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""ProductId"",""ImageUrl"") VALUES ");
            var piParams = new List<object>();
            for (int j = 0; j < count; j++)
            {
                if (j > 0) piBuilder.Append(',');
                var i = b + j;
                var p = piParams.Count;
                piParams.Add(Guid.NewGuid()); piParams.Add(now); piParams.Add(prodIds[i]);
                piBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},'https://picsum.photos/seed/prod{i}/400/400')");
            }
            await ctx.Database.ExecuteSqlRawAsync(piBuilder.ToString(), piParams.ToArray());
        }

        // -- Addresses (90) --
        var addrIds = new List<Guid>();
        var addrResultId = Guid.NewGuid();
        for (int i = 0; i < addrUserIds.Count; i++)
        {
            var id = i == 0 ? addrResultId : Guid.NewGuid();
            addrIds.Add(id);
        }
        for (int b = 0; b < addrUserIds.Count; b += 45)
        {
            var count = Math.Min(45, addrUserIds.Count - b);
            var aBuilder = new StringBuilder();
            aBuilder.Append($@"INSERT INTO ""UserAddresses"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""UserId"",""ReceiverName"",""PhoneNumber"",""AddressLine"",""Ward"",""District"",""Province"",""IsDefault"") VALUES ");
            var aParams = new List<object>();
            for (int j = 0; j < count; j++)
            {
                if (j > 0) aBuilder.Append(',');
                var i = b + j;
                var p = aParams.Count;
                aParams.Add(addrIds[i]); aParams.Add(now); aParams.Add(addrUserIds[i]);
                aBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},'Receiver {i}','09{rand.Next(10000000, 99999999)}','{faker.Address.StreetAddress().Replace("'", "''")}','Ward {rand.Next(1, 25)}','District {rand.Next(1, 25)}','HCMC',{(i == 0).ToString().ToLower()})");
            }
            await ctx.Database.ExecuteSqlRawAsync(aBuilder.ToString(), aParams.ToArray());
        }

        // -- Orders (2 seed + 998 random = 1000) --
        var pendId = Guid.NewGuid();
        var pendCode = rand.Next(100000, 999999);
        var compId = Guid.NewGuid();
        var compCode = rand.Next(100000, 999999);

        await ctx.Database.ExecuteSqlRawAsync(
            $@"INSERT INTO ""Orders"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""UserId"",""TotalAmount"",""DiscountAmount"",""Status"",""OrderCode"",""PaymentMethod"")
              VALUES ({{{0}}},{{{1}}},NULL,NULL,NULL,false,NULL,NULL,{{{2}}},150000,0,'Pending',{{{3}}},'COD')",
            pendId, now, userId, pendCode);
        await ctx.Database.ExecuteSqlRawAsync(
            $@"INSERT INTO ""Orders"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""UserId"",""TotalAmount"",""DiscountAmount"",""Status"",""OrderCode"",""PaymentMethod"")
              VALUES ({{{0}}},{{{1}}},NULL,NULL,NULL,false,NULL,NULL,{{{2}}},150000,0,'Completed',{{{3}}},'COD')",
            compId, now, userId, compCode);

        var orderIds = new List<Guid> { pendId, compId };
        var orderStatuses = new[] { "Pending", "Processing", "Completed", "Cancelled", "Refunded" };
        var orderUserIdsList = new List<Guid> { userId, userId };
        var orderAddressIds = new List<Guid> { addrResultId, addrResultId };
        for (int i = 2; i < 1000; i++)
        {
            orderIds.Add(Guid.NewGuid());
            orderUserIdsList.Add(orderUserIds[i % orderUserIds.Count]);
            orderAddressIds.Add(addrIds[rand.Next(addrIds.Count)]);
        }
        for (int b = 2; b < orderIds.Count; b += 200)
        {
            var count = Math.Min(200, orderIds.Count - b);
            var oBuilder = new StringBuilder();
            oBuilder.Append($@"INSERT INTO ""Orders"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""UserId"",""TotalAmount"",""DiscountAmount"",""Status"",""OrderCode"",""PaymentMethod"") VALUES ");
            var oParams = new List<object>();
            for (int j = 0; j < count; j++)
            {
                if (j > 0) oBuilder.Append(',');
                var i = b + j;
                var status = orderStatuses[rand.Next(orderStatuses.Length)];
                var code = rand.Next(100000, 999999);
                var total = rand.Next(100000, 5000000);
                var p = oParams.Count;
                oParams.Add(orderIds[i]); oParams.Add(now.AddMinutes(-i)); oParams.Add(orderUserIdsList[i]); oParams.Add(code);
                oBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},{total},0,'{status}',@p{p + 3},'COD')");
            }
            await ctx.Database.ExecuteSqlRawAsync(oBuilder.ToString(), oParams.ToArray());
        }

        // -- OrderItems (2 seed + 1998 random = 2000) --
        Guid oi1 = Guid.NewGuid(), oi2 = Guid.NewGuid();
        await ctx.Database.ExecuteSqlRawAsync(
            $@"INSERT INTO ""OrderItems"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""OrderId"",""ProductVariantId"",""Quantity"",""Price"",""ProductSnapshot"")
              VALUES ({{{0}}},{{{1}}},NULL,NULL,NULL,false,NULL,NULL,{{{2}}},{{{3}}},1,150000,'Test Product')",
            oi1, now, pendId, varIdsFirst3[0]);
        await ctx.Database.ExecuteSqlRawAsync(
            $@"INSERT INTO ""OrderItems"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""OrderId"",""ProductVariantId"",""Quantity"",""Price"",""ProductSnapshot"")
              VALUES ({{{0}}},{{{1}}},NULL,NULL,NULL,false,NULL,NULL,{{{2}}},{{{3}}},1,150000,'Test Product')",
            oi2, now, compId, varIdsFirst3[1]);

        for (int b = 0; b < 1998; b += 500)
        {
            var count = Math.Min(500, 1998 - b);
            var oiBuilder = new StringBuilder();
            oiBuilder.Append($@"INSERT INTO ""OrderItems"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""OrderId"",""ProductVariantId"",""Quantity"",""Price"",""ProductSnapshot"") VALUES ");
            var oiParams = new List<object>();
            for (int j = 0; j < count; j++)
            {
                if (j > 0) oiBuilder.Append(',');
                var orderIdx = (b + j) % (orderIds.Count - 2) + 2;
                var varIdx = rand.Next(varIdsFirst3.Count);
                var qty = rand.Next(1, 5);
                var price = rand.Next(50000, 2000000);
                var p = oiParams.Count;
                oiParams.Add(Guid.NewGuid()); oiParams.Add(now); oiParams.Add(orderIds[orderIdx]); oiParams.Add(varIdsFirst3[varIdx]);
                oiBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},@p{p + 3},{qty},{price},'Snapshot {b + j}')");
            }
            await ctx.Database.ExecuteSqlRawAsync(oiBuilder.ToString(), oiParams.ToArray());
        }

        // -- Reviews (5k) --
        for (int b = 0; b < 5000; b += 500)
        {
            var count = Math.Min(500, 5000 - b);
            var rBuilder = new StringBuilder();
            rBuilder.Append($@"INSERT INTO ""Reviews"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""UserId"",""ProductId"",""OrderId"",""Rating"",""Title"",""Comment"",""Status"") VALUES ");
            var rParams = new List<object>();
            for (int j = 0; j < count; j++)
            {
                if (j > 0) rBuilder.Append(',');
                var u = reviewUserIds[rand.Next(reviewUserIds.Count)];
                var prod = prodIds[rand.Next(prodIds.Count)];
                var order = orderIds[rand.Next(orderIds.Count)];
                var rating = rand.Next(1, 6);
                var rawTitle = faker.Commerce.ProductName().Replace("'", "''");
                var title = rawTitle.Length > 20 ? rawTitle.Substring(0, 20) : rawTitle;
                var comment = faker.Lorem.Sentence().Replace("'", "''");
                var p = rParams.Count;
                rParams.Add(Guid.NewGuid()); rParams.Add(now); rParams.Add(u); rParams.Add(prod); rParams.Add(order);
                rBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},@p{p + 3},@p{p + 4},{rating},'{title}','{comment}','Approved')");
            }
            await ctx.Database.ExecuteSqlRawAsync(rBuilder.ToString(), rParams.ToArray());
        }

        // -- Payments (one per order) --
        var payStatuses = new[] { "Pending", "Success", "Failed" };
        for (int b = 2; b < orderIds.Count; b += 200)
        {
            var count = Math.Min(200, orderIds.Count - b);
            var payBuilder = new StringBuilder();
            payBuilder.Append($@"INSERT INTO ""Payments"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""OrderId"",""OrderCode"",""PaymentLinkId"",""Amount"",""Currency"",""Status"") VALUES ");
            var payParams = new List<object>();
            for (int j = 0; j < count; j++)
            {
                if (j > 0) payBuilder.Append(',');
                var i = b + j;
                var payStatus = payStatuses[rand.Next(payStatuses.Length)];
                var linkId = Guid.NewGuid().ToString("N");
                var orderCode = rand.Next(100000, 999999);
                var amount = rand.Next(100000, 5000000);
                var p = payParams.Count;
                payParams.Add(Guid.NewGuid()); payParams.Add(now); payParams.Add(orderIds[i]); payParams.Add(orderCode); payParams.Add(linkId);
                payBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},@p{p + 3},@p{p + 4},{amount},'VND','{payStatus}')");
            }
            await ctx.Database.ExecuteSqlRawAsync(payBuilder.ToString(), payParams.ToArray());
        }

        // -- LoyaltyAccounts (25) --
        var laIds = new List<Guid>();
        for (int i = 0; i < loyaltyUserIds.Count; i++)
            laIds.Add(Guid.NewGuid());
        for (int b = 0; b < laIds.Count; b += 25)
        {
            var count = Math.Min(25, laIds.Count - b);
            var laBuilder = new StringBuilder();
            laBuilder.Append($@"INSERT INTO ""LoyaltyAccounts"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""UserId"",""AvailablePoints"",""PendingPoints"") VALUES ");
            var laParams = new List<object>();
            for (int j = 0; j < count; j++)
            {
                if (j > 0) laBuilder.Append(',');
                var i = b + j;
                var p = laParams.Count;
                laParams.Add(laIds[i]); laParams.Add(now); laParams.Add(loyaltyUserIds[i]);
                laBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},{rand.Next(0, 10000)},{rand.Next(0, 5000)})");
            }
            await ctx.Database.ExecuteSqlRawAsync(laBuilder.ToString(), laParams.ToArray());
        }

        // -- LoyaltyTransactions (200) --
        var transTypes = new[] { "Earn", "Redeem", "Expired" };
        var transStatuses = new[] { "Pending", "Completed" };
        for (int b = 0; b < 200; b += 100)
        {
            var count = Math.Min(100, 200 - b);
            var ltBuilder = new StringBuilder();
            ltBuilder.Append($@"INSERT INTO ""LoyaltyTransactions"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""LoyaltyAccountId"",""Points"",""Type"",""Status"",""Description"") VALUES ");
            var ltParams = new List<object>();
            for (int j = 0; j < count; j++)
            {
                if (j > 0) ltBuilder.Append(',');
                var la = laIds[rand.Next(laIds.Count)];
                var points = rand.Next(10, 500);
                var type = transTypes[rand.Next(transTypes.Length)];
                var status = transStatuses[rand.Next(transStatuses.Length)];
                var p = ltParams.Count;
                ltParams.Add(Guid.NewGuid()); ltParams.Add(now); ltParams.Add(la);
                ltBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},{points},'{type}','{status}','Points {type.ToLower()} for order {rand.Next(100000, 999999)}')");
            }
            await ctx.Database.ExecuteSqlRawAsync(ltBuilder.ToString(), ltParams.ToArray());
        }

        // -- Cart Items (40 users, 1-3 items each ≈ 80 items) --
        for (int i = 0; i < cartUserIds.Count; i++)
        {
            var itemCount = rand.Next(1, 4);
            var ciBuilder = new StringBuilder();
            ciBuilder.Append($@"INSERT INTO ""CartItems"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""UserId"",""ProductVariantId"",""Quantity"") VALUES ");
            var ciParams = new List<object>();
            for (int j = 0; j < itemCount; j++)
            {
                if (j > 0) ciBuilder.Append(',');
                var varIdx = rand.Next(varIdsFirst3.Count);
                var qty = rand.Next(1, 4);
                var p = ciParams.Count;
                ciParams.Add(Guid.NewGuid()); ciParams.Add(now); ciParams.Add(cartUserIds[i]); ciParams.Add(varIdsFirst3[varIdx]);
                ciBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},@p{p + 3},{qty})");
            }
            await ctx.Database.ExecuteSqlRawAsync(ciBuilder.ToString(), ciParams.ToArray());
        }

        // -- Wishlist Items (50 users, 1-5 items each ≈ 150 items) --
        for (int i = 0; i < wishlistUserIds.Count; i++)
        {
            var itemCount = rand.Next(1, 6);
            var wiBuilder = new StringBuilder();
            wiBuilder.Append($@"INSERT INTO ""WishlistItems"" (""Id"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"",""DeletedAt"",""DeletedBy"",""UserId"",""ProductVariantId"") VALUES ");
            var wiParams = new List<object>();
            for (int j = 0; j < itemCount; j++)
            {
                if (j > 0) wiBuilder.Append(',');
                var variant = varIdsFirst3[rand.Next(varIdsFirst3.Count)];
                var p = wiParams.Count;
                wiParams.Add(Guid.NewGuid()); wiParams.Add(now); wiParams.Add(wishlistUserIds[i]); wiParams.Add(variant);
                wiBuilder.Append($"(@p{p},@p{p + 1},NULL,NULL,NULL,false,NULL,NULL,@p{p + 2},@p{p + 3})");
            }
            await ctx.Database.ExecuteSqlRawAsync(wiBuilder.ToString(), wiParams.ToArray());
        }

        return new SeedResult(prodIds[0], varIdsFirst3[0], addrResultId, pendId, pendCode, compId);
    }
}
