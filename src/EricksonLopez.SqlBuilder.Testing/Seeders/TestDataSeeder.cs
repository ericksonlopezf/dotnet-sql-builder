// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.Seeders;

/// <summary>
/// Generates realistic test data for integration tests using the Bogus library.
/// All data is deterministic when a seed is provided.
///
/// Usage:
///   var customers = TestDataSeeder.Customers(100);
///   var products  = TestDataSeeder.Products(500, categories);
///   var orders    = TestDataSeeder.Orders(1000, customers);
///   var items     = TestDataSeeder.OrderItems(5000, orders, products);
/// </summary>
public static class TestDataSeeder
{
    // Deterministic seed for reproducible test data
    private const int DefaultSeed = 42;

    // ─── Customers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a list of fake Customer entities.
    /// Default: 100 records.
    /// </summary>
    public static IReadOnlyList<Customer> Customers(int count = 100, int seed = DefaultSeed)
    {
        Randomizer.Seed = new Random(seed);

        return new Faker<Customer>()
            .RuleFor(c => c.Id,        f => f.IndexFaker + 1)
            .RuleFor(c => c.Name,      f => f.Company.CompanyName())
            .RuleFor(c => c.Email,     f => f.Internet.Email())
            .RuleFor(c => c.Phone,     f => f.Phone.PhoneNumber())
            .RuleFor(c => c.TaxId,     f => f.Random.Bool(0.6f) ? f.Finance.Account(10) : null)
            .RuleFor(c => c.IsActive,  f => f.Random.Bool(0.9f))   // 90% active
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(3).ToUniversalTime())
            .RuleFor(c => c.UpdatedAt, (f, c) => f.Random.Bool(0.4f)
                ? f.Date.Between(c.CreatedAt, DateTime.UtcNow).ToUniversalTime()
                : null)
            .Generate(count);
    }

    // ─── Addresses ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates 1-3 addresses per customer.
    /// </summary>
    public static IReadOnlyList<Address> Addresses(IReadOnlyList<Customer> customers, int seed = DefaultSeed)
    {
        Randomizer.Seed = new Random(seed + 1);
        var result = new List<Address>();
        int id = 1;

        var addrFaker = new Faker<Address>()
            .RuleFor(a => a.Street,     f => f.Address.StreetAddress())
            .RuleFor(a => a.City,       f => f.Address.City())
            .RuleFor(a => a.State,      f => f.Address.StateAbbr())
            .RuleFor(a => a.Country,    f => f.PickRandom("US", "CA", "MX", "GB", "DE"))
            .RuleFor(a => a.PostalCode, f => f.Address.ZipCode())
            .RuleFor(a => a.AddressType, f => f.PickRandom("shipping", "billing"))
            .RuleFor(a => a.IsDefault,  _ => false);

        foreach (var customer in customers)
        {
            int addressCount = new Faker().Random.Int(1, 3);
            for (int i = 0; i < addressCount; i++)
            {
                var addr = addrFaker.Generate();
                addr.Id = id++;
                addr.CustomerId = customer.Id;
                addr.IsDefault = i == 0; // first address is default
                result.Add(addr);
            }
        }
        return result;
    }

    // ─── Categories ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the standard 10-category hierarchy used in DDL seed data.
    /// Used to ensure consistency between DDL and test data.
    /// </summary>
    public static IReadOnlyList<Category> Categories()
    {
        return new List<Category>
        {
            new() { Id = 1, Name = "Electronics",        Slug = "electronics",    ParentCategoryId = null, IsActive = true, SortOrder = 1 },
            new() { Id = 2, Name = "Clothing & Apparel", Slug = "clothing",       ParentCategoryId = null, IsActive = true, SortOrder = 2 },
            new() { Id = 3, Name = "Home & Garden",      Slug = "home-garden",    ParentCategoryId = null, IsActive = true, SortOrder = 3 },
            new() { Id = 4, Name = "Books & Media",      Slug = "books-media",    ParentCategoryId = null, IsActive = true, SortOrder = 4 },
            new() { Id = 5, Name = "Laptops",            Slug = "laptops",        ParentCategoryId = 1,    IsActive = true, SortOrder = 1 },
            new() { Id = 6, Name = "Smartphones",        Slug = "smartphones",    ParentCategoryId = 1,    IsActive = true, SortOrder = 2 },
            new() { Id = 7, Name = "Accessories",        Slug = "accessories",    ParentCategoryId = 1,    IsActive = true, SortOrder = 3 },
            new() { Id = 8, Name = "Men's Clothing",     Slug = "mens-clothing",  ParentCategoryId = 2,    IsActive = true, SortOrder = 1 },
            new() { Id = 9, Name = "Women's Clothing",   Slug = "womens-clothing",ParentCategoryId = 2,    IsActive = true, SortOrder = 2 },
            new() { Id = 10,Name = "Gaming Laptops",     Slug = "gaming-laptops", ParentCategoryId = 5,    IsActive = true, SortOrder = 1 },
        };
    }

    // ─── Products ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a list of fake Product entities.
    /// Default: 500 records.
    /// </summary>
    public static IReadOnlyList<Product> Products(
        int count = 500,
        IReadOnlyList<Category>? categories = null,
        int seed = DefaultSeed)
    {
        Randomizer.Seed = new Random(seed + 2);
        categories ??= Categories();
        var categoryIds = categories.Select(c => c.Id).ToArray();

        return new Faker<Product>()
            .RuleFor(p => p.Id,          f => f.IndexFaker + 1)
            .RuleFor(p => p.CategoryId,  f => f.PickRandom(categoryIds))
            .RuleFor(p => p.Name,        f => f.Commerce.ProductName())
            .RuleFor(p => p.Sku,         f => $"SKU-{f.Random.AlphaNumeric(8).ToUpper()}")
            .RuleFor(p => p.Description, f => f.Random.Bool(0.7f) ? f.Lorem.Paragraph() : null)
            .RuleFor(p => p.Price,       f => Math.Round((decimal)f.Random.Double(5, 2000), 2))
            .RuleFor(p => p.CostPrice,   (f, p) => Math.Round(p.Price * (decimal)f.Random.Double(0.3, 0.8), 2))
            .RuleFor(p => p.Stock,       f => f.Random.Int(0, 500))
            .RuleFor(p => p.MinStock,    f => f.Random.Int(5, 20))
            .RuleFor(p => p.IsActive,    f => f.Random.Bool(0.85f))
            .RuleFor(p => p.CreatedAt,   f => f.Date.Past(2).ToUniversalTime())
            .RuleFor(p => p.UpdatedAt,   (f, p) => f.Random.Bool(0.3f)
                ? f.Date.Between(p.CreatedAt, DateTime.UtcNow).ToUniversalTime()
                : null)
            .Generate(count);
    }

    // ─── Users ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a list of fake User entities.
    /// </summary>
    public static IReadOnlyList<User> Users(int count = 20, int seed = DefaultSeed)
    {
        Randomizer.Seed = new Random(seed + 3);

        return new Faker<User>()
            .RuleFor(u => u.Id,                   f => f.IndexFaker + 1)
            .RuleFor(u => u.Username,              f => f.Internet.UserName())
            .RuleFor(u => u.Email,                 f => f.Internet.Email())
            .RuleFor(u => u.PasswordHash,          _ => "$argon2id$v=19$m=65536,t=3,p=4$" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())))
            .RuleFor(u => u.FirstName,             f => f.Name.FirstName())
            .RuleFor(u => u.LastName,              f => f.Name.LastName())
            .RuleFor(u => u.IsActive,              f => f.Random.Bool(0.9f))
            .RuleFor(u => u.EmailVerified,         f => f.Random.Bool(0.8f))
            .RuleFor(u => u.CreatedAt,             f => f.Date.Past(2).ToUniversalTime())
            .RuleFor(u => u.LastLoginAt,           (f, u) => f.Random.Bool(0.7f)
                ? f.Date.Between(u.CreatedAt, DateTime.UtcNow).ToUniversalTime()
                : null)
            .RuleFor(u => u.FailedLoginAttempts,   f => f.Random.Bool(0.05f) ? f.Random.Int(1, 5) : 0)
            .Generate(count);
    }

    // ─── Orders ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a list of fake Order entities.
    /// Default: 1000 records.
    /// </summary>
    public static IReadOnlyList<Order> Orders(
        int count = 1000,
        IReadOnlyList<Customer>? customers = null,
        int seed = DefaultSeed)
    {
        Randomizer.Seed = new Random(seed + 4);
        customers ??= Customers();
        var customerIds = customers.Select(c => c.Id).ToArray();

        var statuses = new[] { "pending", "confirmed", "shipped", "delivered", "cancelled" };

        return new Faker<Order>()
            .RuleFor(o => o.Id,             f => f.IndexFaker + 1)
            .RuleFor(o => o.CustomerId,     f => f.PickRandom(customerIds))
            .RuleFor(o => o.Status,         f => f.PickRandom(statuses))
            .RuleFor(o => o.Notes,          f => f.Random.Bool(0.2f) ? f.Lorem.Sentence() : null)
            .RuleFor(o => o.TaxAmount,      f => Math.Round((decimal)f.Random.Double(0, 50), 2))
            .RuleFor(o => o.DiscountAmount, f => Math.Round((decimal)f.Random.Double(0, 30), 2))
            .RuleFor(o => o.TotalAmount,    (f, o) => Math.Round((decimal)f.Random.Double(20, 500) + o.TaxAmount - o.DiscountAmount, 2))
            .RuleFor(o => o.Currency,       _ => "USD")
            .RuleFor(o => o.CreatedAt,      f => f.Date.Past(2).ToUniversalTime())
            .RuleFor(o => o.ConfirmedAt,    (f, o) => o.Status != "pending"
                ? f.Date.Between(o.CreatedAt, o.CreatedAt.AddDays(1)).ToUniversalTime()
                : null)
            .RuleFor(o => o.ShippedAt,      (f, o) => o.Status is "shipped" or "delivered"
                ? f.Date.Between(o.CreatedAt.AddDays(1), o.CreatedAt.AddDays(5)).ToUniversalTime()
                : null)
            .RuleFor(o => o.DeliveredAt,    (f, o) => o.Status == "delivered"
                ? f.Date.Between(o.CreatedAt.AddDays(5), o.CreatedAt.AddDays(10)).ToUniversalTime()
                : null)
            .RuleFor(o => o.IsDeleted,      f => f.Random.Bool(0.03f))  // 3% soft-deleted
            .RuleFor(o => o.DeletedAt,      (f, o) => o.IsDeleted
                ? f.Date.Between(o.CreatedAt, DateTime.UtcNow).ToUniversalTime()
                : null)
            .Generate(count);
    }

    // ─── Order Items ──────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a list of fake OrderItem entities.
    /// Default: 5000 records (5 items per order on average).
    /// </summary>
    public static IReadOnlyList<OrderItem> OrderItems(
        int count = 5000,
        IReadOnlyList<Order>? orders = null,
        IReadOnlyList<Product>? products = null,
        int seed = DefaultSeed)
    {
        Randomizer.Seed = new Random(seed + 5);
        orders   ??= Orders();
        products ??= Products();

        var orderIds   = orders.Select(o => o.Id).ToArray();
        var productMap = products.ToDictionary(p => p.Id, p => p.Price);
        var productIds = products.Select(p => p.Id).ToArray();

        return new Faker<OrderItem>()
            .RuleFor(oi => oi.Id,              f => f.IndexFaker + 1)
            .RuleFor(oi => oi.OrderId,         f => f.PickRandom(orderIds))
            .RuleFor(oi => oi.ProductId,       f => f.PickRandom(productIds))
            .RuleFor(oi => oi.Quantity,        f => f.Random.Int(1, 10))
            .RuleFor(oi => oi.UnitPrice,       (f, oi) => productMap.TryGetValue(oi.ProductId, out var price) ? price : 0m)
            .RuleFor(oi => oi.DiscountPercent, f => f.Random.Bool(0.3f) ? Math.Round((decimal)f.Random.Double(5, 25), 2) : 0m)
            .RuleFor(oi => oi.TotalPrice,      (f, oi) => Math.Round(oi.UnitPrice * oi.Quantity * (1 - oi.DiscountPercent / 100), 2))
            .RuleFor(oi => oi.Notes,           f => null)
            .Generate(count);
    }

    // ─── Invoices ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates invoices for completed orders.
    /// </summary>
    public static IReadOnlyList<Invoice> Invoices(
        IReadOnlyList<Order>? orders = null,
        int seed = DefaultSeed)
    {
        Randomizer.Seed = new Random(seed + 6);
        orders ??= Orders();

        var invoiceableOrders = orders
            .Where(o => o.Status is "confirmed" or "shipped" or "delivered" && !o.IsDeleted)
            .ToList();

        var statuses = new[] { "draft", "issued", "paid", "overdue" };
        var result = new List<Invoice>();
        int id = 1;

        foreach (var order in invoiceableOrders)
        {
            var f = new Faker();
            var status = f.PickRandom(statuses);
            var issuedAt = order.ConfirmedAt ?? order.CreatedAt.AddDays(1);
            var dueAt = issuedAt.AddDays(30);

            result.Add(new Invoice
            {
                Id = id++,
                OrderId = order.Id,
                InvoiceNumber = $"INV-{issuedAt:yyyyMM}-{id:D6}",
                Status = status,
                SubtotalAmount = order.TotalAmount - order.TaxAmount + order.DiscountAmount,
                TaxAmount = order.TaxAmount,
                TotalAmount = order.TotalAmount,
                PaidAmount = status == "paid" ? order.TotalAmount : 0m,
                Currency = order.Currency,
                IssuedAt = issuedAt,
                DueAt = dueAt,
                PaidAt = status == "paid" ? dueAt.AddDays(-f.Random.Int(1, 15)) : null,
            });
        }
        return result;
    }

    // ─── Payments ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates payment records for paid invoices.
    /// </summary>
    public static IReadOnlyList<Payment> Payments(
        IReadOnlyList<Invoice>? invoices = null,
        int seed = DefaultSeed)
    {
        Randomizer.Seed = new Random(seed + 7);
        invoices ??= Invoices();

        var paidInvoices = invoices.Where(i => i.Status == "paid").ToList();
        var methods = new[] { "credit_card", "bank_transfer", "paypal", "stripe" };
        var result = new List<Payment>();
        int id = 1;

        foreach (var invoice in paidInvoices)
        {
            var f = new Faker();
            result.Add(new Payment
            {
                Id = id++,
                InvoiceId = invoice.Id,
                Amount = invoice.TotalAmount,
                Method = f.PickRandom(methods),
                Status = "completed",
                TransactionRef = $"TXN-{Guid.NewGuid():N}".ToUpper().Substring(0, 20),
                PaidAt = invoice.PaidAt ?? invoice.DueAt,
            });
        }
        return result;
    }

    // ─── Full Dataset ─────────────────────────────────────────────────────────

    /// <summary>
    /// Generates the complete standard dataset:
    /// 100 customers, 500 products, 1000 orders, 5000 order items.
    /// </summary>
    public static StandardDataset Generate(int seed = DefaultSeed)
    {
        var customers  = Customers(100, seed);
        var categories = Categories();
        var products   = Products(500, categories, seed);
        var orders     = Orders(1000, customers, seed);
        var items      = OrderItems(5000, orders, products, seed);
        var invoices   = Invoices(orders, seed);
        var payments   = Payments(invoices, seed);

        return new StandardDataset(customers, categories, products, orders, items, invoices, payments);
    }
}
