// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Fluent test data builder for <see cref="Order"/>.
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

    public static OrderBuilder Create() => new();

    public OrderBuilder WithId(int id) { _id = id; return this; }
    public OrderBuilder WithCustomerId(int customerId) { _customerId = customerId; return this; }
    public OrderBuilder WithStatus(string status) { _status = status; return this; }
    public OrderBuilder WithTotalAmount(decimal totalAmount) { _totalAmount = totalAmount; _taxAmount = totalAmount * 0.1m; return this; }
    public OrderBuilder WithCurrency(string currency) { _currency = currency; return this; }
    public OrderBuilder WithDeleted(bool isDeleted) { _isDeleted = isDeleted; return this; }

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
