// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Provides a fluent test data builder for creating <see cref="Order"/> instances.
/// </summary>
public sealed class OrderBuilder
{
    private int _id = 1;
    private int _customerId = 1;
    private string _status = "pending";
    private string? _notes = null;
    private decimal _totalAmount = 150.00m;
    private decimal _taxAmount = 15.00m;
    private decimal _discountAmount = 0m;
    private string _currency = "USD";
    private DateTime _createdAt = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private bool _isDeleted = false;

    /// <summary>
    /// Creates a new instance of the <see cref="OrderBuilder"/> class.
    /// </summary>
    /// <returns>A new <see cref="OrderBuilder"/> instance.</returns>
    public static OrderBuilder Create() => new();

    /// <summary>
    /// Sets the identifier for the order being built.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public OrderBuilder WithId(int id) { _id = id; return this; }

    /// <summary>
    /// Sets the customer identifier for the order being built.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public OrderBuilder WithCustomerId(int customerId) { _customerId = customerId; return this; }

    /// <summary>
    /// Sets the order lifecycle status for the order being built.
    /// </summary>
    /// <param name="status">The order status.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public OrderBuilder WithStatus(string status) { _status = status; return this; }

    /// <summary>
    /// Sets the total monetary amount for the order being built.
    /// </summary>
    /// <param name="totalAmount">The total order amount.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public OrderBuilder WithTotalAmount(decimal totalAmount) { _totalAmount = totalAmount; _taxAmount = totalAmount * 0.1m; return this; }

    /// <summary>
    /// Sets the currency code for the order being built.
    /// </summary>
    /// <param name="currency">The currency code.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public OrderBuilder WithCurrency(string currency) { _currency = currency; return this; }

    /// <summary>
    /// Sets a value indicating whether the order being built is marked as soft-deleted.
    /// </summary>
    /// <param name="isDeleted">A value indicating whether the order is soft-deleted.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public OrderBuilder WithDeleted(bool isDeleted) { _isDeleted = isDeleted; return this; }

    /// <summary>
    /// Builds and returns a new <see cref="Order"/> instance with the configured values.
    /// </summary>
    /// <returns>A configured <see cref="Order"/> instance.</returns>
    public Order Build() => new()
    {
        Id = _id,
        CustomerId = _customerId,
        Status = _status,
        Notes = _notes,
        TotalAmount = _totalAmount,
        TaxAmount = _taxAmount,
        DiscountAmount = _discountAmount,
        Currency = _currency,
        CreatedAt = _createdAt,
        IsDeleted = _isDeleted
    };
}
