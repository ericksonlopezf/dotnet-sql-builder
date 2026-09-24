// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.Seeders;

/// <summary>
/// Represents a complete standard dataset for integration testing scenarios.
/// </summary>
public sealed class StandardDataset
{
    /// <summary>
    /// Gets the collection of seeded customer entities.
    /// </summary>
    public IReadOnlyList<Customer> Customers { get; }

    /// <summary>
    /// Gets the collection of seeded category entities.
    /// </summary>
    public IReadOnlyList<Category> Categories { get; }

    /// <summary>
    /// Gets the collection of seeded product entities.
    /// </summary>
    public IReadOnlyList<Product> Products { get; }

    /// <summary>
    /// Gets the collection of seeded order entities.
    /// </summary>
    public IReadOnlyList<Order> Orders { get; }

    /// <summary>
    /// Gets the collection of seeded order item entities.
    /// </summary>
    public IReadOnlyList<OrderItem> OrderItems { get; }

    /// <summary>
    /// Gets the collection of seeded invoice entities.
    /// </summary>
    public IReadOnlyList<Invoice> Invoices { get; }

    /// <summary>
    /// Gets the collection of seeded payment entities.
    /// </summary>
    public IReadOnlyList<Payment> Payments { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardDataset"/> class with the specified seeded entity collections.
    /// </summary>
    /// <param name="customers">The collection of seeded customer entities.</param>
    /// <param name="categories">The collection of seeded category entities.</param>
    /// <param name="products">The collection of seeded product entities.</param>
    /// <param name="orders">The collection of seeded order entities.</param>
    /// <param name="orderItems">The collection of seeded order item entities.</param>
    /// <param name="invoices">The collection of seeded invoice entities.</param>
    /// <param name="payments">The collection of seeded payment entities.</param>
    public StandardDataset(
        IReadOnlyList<Customer> customers,
        IReadOnlyList<Category> categories,
        IReadOnlyList<Product> products,
        IReadOnlyList<Order> orders,
        IReadOnlyList<OrderItem> orderItems,
        IReadOnlyList<Invoice> invoices,
        IReadOnlyList<Payment> payments)
    {
        Customers = customers;
        Categories = categories;
        Products = products;
        Orders = orders;
        OrderItems = orderItems;
        Invoices = invoices;
        Payments = payments;
    }
}
