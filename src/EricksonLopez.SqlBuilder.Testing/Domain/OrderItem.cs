// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a line item in a customer order.
/// Maps to: order_items table
/// </summary>
[SqlEntity("order_items")]
public partial class OrderItem
{
    [DatabaseGenerated]
    public int Id { get; set; }
    [Indexed]
    public int OrderId { get; set; }
    [Indexed]
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

