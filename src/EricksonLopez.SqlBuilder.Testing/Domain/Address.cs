// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a shipping or billing address associated with a customer.
/// Maps to: addresses table
/// </summary>
[SqlEntity("addresses")]
public partial class Address
{
    [DatabaseGenerated]
    public int Id { get; set; }
    [Indexed]
    public int CustomerId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string AddressType { get; set; } = "shipping"; // shipping, billing
    public bool IsDefault { get; set; } = false;
}

