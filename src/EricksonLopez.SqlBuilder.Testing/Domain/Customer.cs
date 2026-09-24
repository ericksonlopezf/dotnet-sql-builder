// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents a business customer (B2B or B2C).</summary>
[SqlEntity("customers")]
public partial class Customer
{
    /// <summary>Gets or sets the primary key identifier of the customer.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the full legal or trade name of the customer.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the primary contact email address of the customer.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the contact telephone number of the customer.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the tax identification or VAT number of the customer.</summary>
    public string? TaxId { get; set; }

    /// <summary>Gets or sets a value indicating whether the customer account is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp when the customer record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the optional UTC timestamp when the customer record was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}


