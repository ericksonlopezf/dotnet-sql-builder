// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a payment transaction applied to an invoice.
/// Maps to: payments table
/// </summary>
[SqlEntity("payments")]
public partial class Payment
{
    [DatabaseGenerated]
    public int Id { get; set; }
    [Indexed]
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty; // credit_card, bank_transfer, paypal, stripe
    public string Status { get; set; } = "pending"; // pending, completed, failed, refunded
    public string? TransactionRef { get; set; }
    public string? GatewayResponse { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundedAmount { get; set; }
}


