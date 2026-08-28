// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Fluent test data builder for <see cref="Customer"/>.
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

    public static CustomerBuilder Create() => new();

    public CustomerBuilder WithId(int id) { _id = id; return this; }
    public CustomerBuilder WithName(string name) { _name = name; return this; }
    public CustomerBuilder WithEmail(string email) { _email = email; return this; }
    public CustomerBuilder WithPhone(string phone) { _phone = phone; return this; }
    public CustomerBuilder WithActive(bool isActive) { _isActive = isActive; return this; }

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
