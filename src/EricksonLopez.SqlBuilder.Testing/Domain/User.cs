// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents an application user authentication identity.</summary>
[SqlEntity("users")]
public partial class User
{
    /// <summary>Gets or sets the primary key identifier of the user.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the unique username for login authentication.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets the primary email address of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the secure cryptographic hash of the user's password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional first name of the user.</summary>
    public string? FirstName { get; set; }

    /// <summary>Gets or sets the optional last name or surname of the user.</summary>
    public string? LastName { get; set; }

    /// <summary>Gets or sets a value indicating whether the user account is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the user's email address has been verified.</summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>Gets or sets the UTC timestamp when the user account was registered.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the optional UTC timestamp of the user's most recent login.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Gets or sets the optional UTC timestamp until which the user account is temporarily locked.</summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>Gets or sets the count of consecutive failed authentication attempts.</summary>
    public int FailedLoginAttempts { get; set; } = 0;
}


