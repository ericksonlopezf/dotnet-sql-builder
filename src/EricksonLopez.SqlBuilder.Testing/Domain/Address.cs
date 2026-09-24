// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents a shipping or billing address associated with a customer.</summary>
[SqlEntity("addresses")]
public partial class Address
{
    /// <summary>Gets or sets the primary key identifier of the address.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the customer associated with this address.</summary>
    [Indexed]
    public int CustomerId { get; set; }

    /// <summary>Gets or sets the street line of the address.</summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>Gets or sets the city of the address.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the state, province, or region of the address.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the country of the address.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code or ZIP code of the address.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the address type classification, such as shipping or billing.</summary>
    public string AddressType { get; set; } = "shipping"; // shipping, billing

    /// <summary>Gets or sets a value indicating whether this is the default address for the customer.</summary>
    public bool IsDefault { get; set; } = false;
}

