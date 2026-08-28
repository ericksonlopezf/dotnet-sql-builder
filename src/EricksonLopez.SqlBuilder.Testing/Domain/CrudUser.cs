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
    [DatabaseGenerated]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int Age { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
