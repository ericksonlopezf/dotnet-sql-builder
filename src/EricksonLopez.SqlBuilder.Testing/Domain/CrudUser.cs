// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a user entity for integration CRUD tests.
/// Maps to the 'users' table across integration test fixtures.
/// </summary>
[SqlEntity("users")]
public partial class CrudUser
{
    /// <summary>Gets or sets the primary key identifier of the user.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }
    
    /// <summary>Gets or sets the full name of the user.</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the age in years of the user.</summary>
    public int Age { get; set; }
    
    /// <summary>Gets or sets a value indicating whether the user is active.</summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>Gets or sets the UTC timestamp when the user was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
