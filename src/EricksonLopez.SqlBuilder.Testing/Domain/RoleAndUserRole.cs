// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents an authorization role.
/// Maps to: roles table
/// </summary>
[SqlEntity("roles")]
public partial class Role
{
    [DatabaseGenerated]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // admin, manager, viewer, support
    public string? Description { get; set; }
    public string Permissions { get; set; } = "[]"; // JSON array of permission strings
    public bool IsSystem { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Junction table for many-to-many User-Role relationship.
/// Maps to: user_roles table
/// </summary>
[SqlEntity("user_roles")]
public partial class UserRole
{
    [Indexed]
    public int UserId { get; set; }
    [Indexed]
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public int? AssignedByUserId { get; set; }
}


