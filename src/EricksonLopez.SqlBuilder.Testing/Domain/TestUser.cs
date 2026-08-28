// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents a test user entity mapped to the <c>test_users</c> database table,
/// used in integration and unit tests that exercise SQL generation and CRUD operations.
/// </summary>
[SqlEntity("test_users")]
public partial class TestUser
{
    /// <summary>Gets or sets the database-generated primary key identifier.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the display name of the user.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the age of the user.</summary>
    public int Age { get; set; }

    /// <summary>Gets or sets a value indicating whether the user account is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the user record was created.</summary>
    public DateTime CreatedAt { get; set; }
}
