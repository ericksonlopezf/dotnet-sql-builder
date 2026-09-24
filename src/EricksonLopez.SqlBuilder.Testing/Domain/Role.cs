// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents an authorization role.</summary>
[SqlEntity("roles")]
public partial class Role
{
    /// <summary>Gets or sets the primary key identifier of the role.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the unique name of the role, such as admin, manager, or viewer.</summary>
    public string Name { get; set; } = string.Empty; // admin, manager, viewer, support

    /// <summary>Gets or sets an optional description explaining the responsibilities of the role.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the serialized JSON array of permission strings granted to the role.</summary>
    public string Permissions { get; set; } = "[]"; // JSON array of permission strings

    /// <summary>Gets or sets a value indicating whether this is a built-in immutable system role.</summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>Gets or sets the UTC timestamp when the role was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
