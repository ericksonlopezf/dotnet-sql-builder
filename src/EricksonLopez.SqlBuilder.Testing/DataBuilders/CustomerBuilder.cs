// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Provides a fluent test data builder for creating <see cref="Customer"/> instances.
/// </summary>
public sealed class CustomerBuilder
{
    private int _id = 1;
    private string _name = "Acme Corp";
    private string _email = "contact@acme.com";
    private string? _phone = "+1-555-0100";
    private string? _taxId = "TAX-000001";
    private bool _isActive = true;
    private DateTime _createdAt = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the <see cref="CustomerBuilder"/> class.
    /// </summary>
    /// <returns>A new <see cref="CustomerBuilder"/> instance.</returns>
    public static CustomerBuilder Create() => new();

    /// <summary>
    /// Sets the identifier for the customer being built.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public CustomerBuilder WithId(int id) { _id = id; return this; }

    /// <summary>
    /// Sets the name for the customer being built.
    /// </summary>
    /// <param name="name">The customer name.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public CustomerBuilder WithName(string name) { _name = name; return this; }

    /// <summary>
    /// Sets the email address for the customer being built.
    /// </summary>
    /// <param name="email">The customer email address.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public CustomerBuilder WithEmail(string email) { _email = email; return this; }

    /// <summary>
    /// Sets the phone number for the customer being built.
    /// </summary>
    /// <param name="phone">The customer phone number.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public CustomerBuilder WithPhone(string phone) { _phone = phone; return this; }

    /// <summary>
    /// Sets the active status for the customer being built.
    /// </summary>
    /// <param name="isActive">A value indicating whether the customer is active.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public CustomerBuilder WithActive(bool isActive) { _isActive = isActive; return this; }

    /// <summary>
    /// Builds and returns a new <see cref="Customer"/> instance with the configured values.
    /// </summary>
    /// <returns>A configured <see cref="Customer"/> instance.</returns>
    public Customer Build() => new()
    {
        Id = _id,
        Name = _name,
        Email = _email,
        Phone = _phone,
        TaxId = _taxId,
        IsActive = _isActive,
        CreatedAt = _createdAt
    };
}
