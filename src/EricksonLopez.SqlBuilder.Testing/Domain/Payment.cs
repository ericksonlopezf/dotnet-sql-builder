// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents a payment transaction applied to an invoice.</summary>
[SqlEntity("payments")]
public partial class Payment
{
    /// <summary>Gets or sets the primary key identifier of the payment.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the invoice being paid.</summary>
    [Indexed]
    public int InvoiceId { get; set; }

    /// <summary>Gets or sets the monetary amount processed in the payment transaction.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the payment method used, such as credit_card, bank_transfer, paypal, or stripe.</summary>
    public string Method { get; set; } = string.Empty; // credit_card, bank_transfer, paypal, stripe

    /// <summary>Gets or sets the transaction status, such as pending, completed, failed, or refunded.</summary>
    public string Status { get; set; } = "pending"; // pending, completed, failed, refunded

    /// <summary>Gets or sets the external transaction or gateway reference number.</summary>
    public string? TransactionRef { get; set; }

    /// <summary>Gets or sets the raw response payload returned by the payment gateway.</summary>
    public string? GatewayResponse { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the payment was processed.</summary>
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the optional UTC timestamp when the payment was refunded.</summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>Gets or sets the optional monetary amount that was refunded.</summary>
    public decimal? RefundedAmount { get; set; }
}


