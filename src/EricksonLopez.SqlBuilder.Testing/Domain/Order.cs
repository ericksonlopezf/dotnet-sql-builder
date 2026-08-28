// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a customer purchase order.
/// Supports soft delete (is_deleted + deleted_at).
/// Maps to: orders table
/// </summary>
[SqlEntity("orders")]
public partial class Order
{
    [DatabaseGenerated]
    public int Id { get; set; }
    [Indexed]
    public int CustomerId { get; set; }
    public string Status { get; set; } = "pending"; // pending, confirmed, shipped, delivered, cancelled
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}


