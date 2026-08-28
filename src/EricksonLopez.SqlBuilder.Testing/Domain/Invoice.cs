// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a tax invoice issued for an order.
/// Maps to: invoices table
/// </summary>
[SqlEntity("invoices")]
public partial class Invoice
{
    [DatabaseGenerated]
    public int Id { get; set; }
    [Indexed]
    public int OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "draft"; // draft, issued, paid, overdue, cancelled
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public string Currency { get; set; } = "USD";
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
}


