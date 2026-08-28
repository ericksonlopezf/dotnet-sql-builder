// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a user entity for Oracle integration CRUD tests.
/// Maps to the 'TEST_USERS' table in Oracle.
/// </summary>
[SqlEntity("TEST_USERS")]
public partial class OracleCrudUser
{
    [DatabaseGenerated]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int Age { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
