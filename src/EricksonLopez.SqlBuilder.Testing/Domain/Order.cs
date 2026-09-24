// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents a customer purchase order.</summary>
[SqlEntity("orders")]
public partial class Order
{
    /// <summary>Gets or sets the primary key identifier of the order.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the customer who placed the order.</summary>
    [Indexed]
    public int CustomerId { get; set; }

    /// <summary>Gets or sets the order lifecycle status, such as pending, confirmed, shipped, delivered, or cancelled.</summary>
    public string Status { get; set; } = "pending"; // pending, confirmed, shipped, delivered, cancelled

    /// <summary>Gets or sets optional delivery or order instructions.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the total monetary amount of the order, including taxes and discounts.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the total tax amount applied to the order.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the total promotional discount amount deducted from the order.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Gets or sets the three-letter ISO currency code for the order amounts.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the UTC timestamp when the order was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the optional UTC timestamp when the order was confirmed.</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Gets or sets the optional UTC timestamp when the order was dispatched.</summary>
    public DateTime? ShippedAt { get; set; }

    /// <summary>Gets or sets the optional UTC timestamp when the order was delivered.</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the order has been soft-deleted.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>Gets or sets the optional UTC timestamp when the order was soft-deleted.</summary>
    public DateTime? DeletedAt { get; set; }
}


