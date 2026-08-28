// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.Seeders;

/// <summary>
/// Complete dataset for integration tests.
/// </summary>
public sealed class StandardDataset
{
    public IReadOnlyList<Customer> Customers { get; }
    public IReadOnlyList<Category> Categories { get; }
    public IReadOnlyList<Product> Products { get; }
    public IReadOnlyList<Order> Orders { get; }
    public IReadOnlyList<OrderItem> OrderItems { get; }
    public IReadOnlyList<Invoice> Invoices { get; }
    public IReadOnlyList<Payment> Payments { get; }

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
