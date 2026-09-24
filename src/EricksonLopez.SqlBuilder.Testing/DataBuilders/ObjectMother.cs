// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Provides pre-configured domain entity instances for testing scenarios.
/// </summary>
public static class ObjectMother
{
    /// <summary>
    /// Creates a pre-configured <see cref="User"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the user.</param>
    /// <param name="name">The username and first name of the user.</param>
    /// <param name="isActive"><see langword="true"/> if the user is active; otherwise, <see langword="false"/>.</param>
    /// <returns>A new <see cref="User"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="TestEntity"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    /// <param name="name">The entity name.</param>
    /// <param name="isActive"><see langword="true"/> if the entity is active; otherwise, <see langword="false"/>.</param>
    /// <returns>A new <see cref="TestEntity"/> instance initialized with test data.</returns>
    public static TestEntity CreateTestEntity(int id = 1, string name = "TestEntity", bool isActive = true)
    {
        return new TestEntity
        {
            Id = id,
            Name = name,
            IsActive = isActive
        };
    }

    /// <summary>
    /// Creates a pre-configured <see cref="Product"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the product.</param>
    /// <param name="name">The product name.</param>
    /// <param name="price">The retail price of the product.</param>
    /// <param name="stock">The quantity available in stock.</param>
    /// <param name="categoryId">The identifier of the product category.</param>
    /// <returns>A new <see cref="Product"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="Order"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the order.</param>
    /// <param name="customerId">The identifier of the customer placing the order.</param>
    /// <param name="totalAmount">The total order amount.</param>
    /// <param name="status">The order status string.</param>
    /// <returns>A new <see cref="Order"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="OrderItem"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the order item.</param>
    /// <param name="orderId">The parent order identifier.</param>
    /// <param name="productId">The purchased product identifier.</param>
    /// <param name="quantity">The purchased quantity.</param>
    /// <param name="unitPrice">The price per unit.</param>
    /// <returns>A new <see cref="OrderItem"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="Customer"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the customer.</param>
    /// <param name="name">The customer name.</param>
    /// <param name="email">The customer email address.</param>
    /// <param name="isActive"><see langword="true"/> if the customer is active; otherwise, <see langword="false"/>.</param>
    /// <returns>A new <see cref="Customer"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="Address"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the address.</param>
    /// <param name="customerId">The identifier of the customer associated with the address.</param>
    /// <param name="addressType">The classification type of the address (e.g., shipping or billing).</param>
    /// <returns>A new <see cref="Address"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="Category"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the category.</param>
    /// <param name="name">The display name of the category.</param>
    /// <returns>A new <see cref="Category"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="Invoice"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the invoice.</param>
    /// <param name="orderId">The parent order identifier.</param>
    /// <param name="amount">The total billed invoice amount.</param>
    /// <returns>A new <see cref="Invoice"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="Payment"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the payment transaction.</param>
    /// <param name="invoiceId">The identifier of the paid invoice.</param>
    /// <param name="amount">The payment transaction amount.</param>
    /// <returns>A new <see cref="Payment"/> instance initialized with test data.</returns>
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

    /// <summary>
    /// Creates a pre-configured <see cref="AuditLog"/> instance with optional overrides.
    /// </summary>
    /// <param name="id">The unique identifier for the audit record.</param>
    /// <param name="entityName">The name of the audited entity.</param>
    /// <param name="action">The audited operation name.</param>
    /// <returns>A new <see cref="AuditLog"/> instance initialized with test data.</returns>
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
