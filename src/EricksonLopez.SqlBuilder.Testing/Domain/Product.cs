// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents a product in the catalog.</summary>
[SqlEntity("products")]
public partial class Product
{
    /// <summary>Gets or sets the primary key identifier of the product.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the category identifier to which this product belongs.</summary>
    [Indexed]
    public int CategoryId { get; set; }

    /// <summary>Gets or sets the display name of the product.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the Stock Keeping Unit (SKU) identifying the product variant.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets optional descriptive text or marketing copy for the product.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the retail sales price of the product.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the wholesale or acquisition cost price of the product.</summary>
    public decimal CostPrice { get; set; }

    /// <summary>Gets or sets the currently available inventory quantity in stock.</summary>
    public int Stock { get; set; } = 0;

    /// <summary>Gets or sets the minimum inventory reorder threshold quantity.</summary>
    public int MinStock { get; set; } = 0;

    /// <summary>Gets or sets a value indicating whether the product is active for sales.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp when the product was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the optional UTC timestamp when the product was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}


