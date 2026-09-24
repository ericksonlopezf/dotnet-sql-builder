// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents a tax invoice issued for an order.</summary>
[SqlEntity("invoices")]
public partial class Invoice
{
    /// <summary>Gets or sets the primary key identifier of the invoice.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the order associated with this invoice.</summary>
    [Indexed]
    public int OrderId { get; set; }

    /// <summary>Gets or sets the alphanumeric reference number assigned to the invoice.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the status of the invoice, such as draft, issued, paid, overdue, or cancelled.</summary>
    public string Status { get; set; } = "draft"; // draft, issued, paid, overdue, cancelled

    /// <summary>Gets or sets the pre-tax subtotal amount of the invoice.</summary>
    public decimal SubtotalAmount { get; set; }

    /// <summary>Gets or sets the total tax amount applied to the invoice.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the total amount payable on the invoice, including taxes.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the amount that has been paid toward the invoice balance.</summary>
    public decimal PaidAmount { get; set; } = 0;

    /// <summary>Gets or sets the three-letter ISO currency code for the invoice amounts.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the UTC timestamp when the invoice was issued.</summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp by which payment on the invoice is due.</summary>
    public DateTime DueAt { get; set; }

    /// <summary>Gets or sets the optional UTC timestamp when the invoice was fully paid.</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>Gets or sets optional remarks, payment terms, or notes on the invoice.</summary>
    public string? Notes { get; set; }
}


