// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Centralized Object Mother providing pre-configured domain entity instances for testing.
/// </summary>
public static class ObjectMother
{
    public static User CreateUser(int id = 1, string name = "TestUser", bool isActive = true)
    {
        return new User
        {
            Id = id,
            Username = name,
            Email = $"{name}@example.com",
            PasswordHash = "hash123",
            FirstName = name,
            LastName = "TestLastName",
            IsActive = isActive,
            EmailVerified = true,
            CreatedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            FailedLoginAttempts = 0
        };
    }

    public static TestEntity CreateTestEntity(int id = 1, string name = "TestEntity", bool isActive = true)
    {
        return new TestEntity
        {
            Id = id,
            Name = name,
            IsActive = isActive
        };
    }

    public static Product CreateProduct(int id = 1, string name = "Laptop", decimal price = 999.99m, int stock = 50, int categoryId = 1)
    {
        return new Product
        {
            Id = id,
            CategoryId = categoryId,
            Name = name,
            Sku = $"SKU-{id:D5}",
            Description = $"Description for {name}",
            Price = price,
            CostPrice = price * 0.7m,
            Stock = stock,
            MinStock = 5,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    public static Order CreateOrder(int id = 1, int customerId = 1, decimal totalAmount = 150.00m, string status = "pending")
    {
        return new Order
        {
            Id = id,
            CustomerId = customerId,
            Status = status,
            TotalAmount = totalAmount,
            TaxAmount = totalAmount * 0.1m,
            DiscountAmount = 0m,
            Currency = "USD",
            CreatedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        };
    }

    public static OrderItem CreateOrderItem(int id = 1, int orderId = 1, int productId = 1, int quantity = 2, decimal unitPrice = 75.00m)
    {
        return new OrderItem
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice
        };
    }

    public static Customer CreateCustomer(int id = 1, string name = "Acme Corp", string email = "contact@acme.com", bool isActive = true)
    {
        return new Customer
        {
            Id = id,
            Name = name,
            Email = email,
            Phone = "+1-555-0100",
            TaxId = $"TAX-{id:D6}",
            IsActive = isActive,
            CreatedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    public static Address CreateAddress(int id = 1, int customerId = 1, string addressType = "shipping")
    {
        return new Address
        {
            Id = id,
            CustomerId = customerId,
            AddressType = addressType,
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "US",
            IsDefault = true
        };
    }

    public static Category CreateCategory(int id = 1, string name = "Electronics")
    {
        return new Category
        {
            Id = id,
            Name = name,
            Slug = name.ToLowerInvariant().Replace(' ', '-'),
            ParentCategoryId = null,
            IsActive = true,
            SortOrder = 0
        };
    }

    public static Invoice CreateInvoice(int id = 1, int orderId = 1, decimal amount = 150.00m)
    {
        return new Invoice
        {
            Id = id,
            OrderId = orderId,
            InvoiceNumber = $"INV-{id:D6}",
            Status = "issued",
            SubtotalAmount = amount * 0.9m,
            TaxAmount = amount * 0.1m,
            TotalAmount = amount,
            PaidAmount = 0,
            Currency = "USD",
            IssuedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            DueAt = DateTime.UtcNow.AddDays(30)
        };
    }

    public static Payment CreatePayment(int id = 1, int invoiceId = 1, decimal amount = 150.00m)
    {
        return new Payment
        {
            Id = id,
            InvoiceId = invoiceId,
            Amount = amount,
            Method = "credit_card",
            Status = "completed",
            TransactionRef = $"TXN-{id:D8}",
            PaidAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    public static AuditLog CreateAuditLog(int id = 1, string entityName = "User", string action = "CREATE")
    {
        return new AuditLog
        {
            Id = id,
            EntityName = entityName,
            EntityId = "1",
            Action = action,
            UserId = 1,
            Timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }
}

[EricksonLopez.SqlBuilder.Annotations.SqlEntity("testentitys")]
public partial class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}
