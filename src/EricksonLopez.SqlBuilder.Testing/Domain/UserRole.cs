// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a junction table entry for a many-to-many user-role relationship.
/// </summary>
[SqlEntity("user_roles")]
public partial class UserRole
{
    /// <summary>Gets or sets the identifier of the assigned user.</summary>
    [Indexed]
    public int UserId { get; set; }

    /// <summary>Gets or sets the identifier of the assigned role.</summary>
    [Indexed]
    public int RoleId { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the role was assigned to the user.</summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the optional identifier of the administrator who assigned the role.</summary>
    public int? AssignedByUserId { get; set; }
}
