// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents a line item in a customer order.</summary>
[SqlEntity("order_items")]
public partial class OrderItem
{
    /// <summary>Gets or sets the primary key identifier of the order item.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the order this item belongs to.</summary>
    [Indexed]
    public int OrderId { get; set; }

    /// <summary>Gets or sets the identifier of the ordered product.</summary>
    [Indexed]
    public int ProductId { get; set; }

    /// <summary>Gets or sets the number of units ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit price of the product at the time of purchase.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the discount percentage applied to this line item.</summary>
    public decimal DiscountPercent { get; set; } = 0;

    /// <summary>Gets or sets the calculated total line price after discounts.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Gets or sets optional item-level customization or notes.</summary>
    public string? Notes { get; set; }
}

