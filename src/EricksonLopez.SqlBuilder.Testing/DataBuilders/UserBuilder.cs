// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Provides a fluent test data builder for creating <see cref="User"/> instances.
/// </summary>
public sealed class UserBuilder
{
    private int _id = 1;
    private string _username = "TestUser";
    private string _email = "testuser@example.com";
    private string _passwordHash = "hash123";
    private string _firstName = "Test";
    private string _lastName = "User";
    private bool _isActive = true;
    private bool _emailVerified = true;
    private DateTime _createdAt = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private int _failedLoginAttempts = 0;

    /// <summary>
    /// Creates a new instance of the <see cref="UserBuilder"/> class.
    /// </summary>
    /// <returns>A new <see cref="UserBuilder"/> instance.</returns>
    public static UserBuilder Create() => new();

    /// <summary>
    /// Sets the identifier for the user being built.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public UserBuilder WithId(int id) { _id = id; return this; }

    /// <summary>
    /// Sets the username for the user being built, and automatically derives an email address.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public UserBuilder WithUsername(string username) { _username = username; _email = $"{username.ToLowerInvariant()}@example.com"; return this; }

    /// <summary>
    /// Sets the email address for the user being built.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public UserBuilder WithEmail(string email) { _email = email; return this; }

    /// <summary>
    /// Sets a value indicating whether the user being built is active.
    /// </summary>
    /// <param name="isActive"><see langword="true"/> if the user is active; otherwise, <see langword="false"/>.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public UserBuilder WithActive(bool isActive) { _isActive = isActive; return this; }

    /// <summary>
    /// Sets the count of failed login attempts for the user being built.
    /// </summary>
    /// <param name="count">The number of failed login attempts.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public UserBuilder WithFailedLoginAttempts(int count) { _failedLoginAttempts = count; return this; }

    /// <summary>
    /// Sets the creation timestamp for the user being built.
    /// </summary>
    /// <param name="createdAt">The creation date and time in UTC.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public UserBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }

    /// <summary>
    /// Builds and returns the configured <see cref="User"/> instance.
    /// </summary>
    /// <returns>A new <see cref="User"/> instance initialized with configured properties.</returns>
    public User Build() => new()
    {
        Id = _id,
        Username = _username,
        Email = _email,
        PasswordHash = _passwordHash,
        FirstName = _firstName,
        LastName = _lastName,
        IsActive = _isActive,
        EmailVerified = _emailVerified,
        CreatedAt = _createdAt,
        FailedLoginAttempts = _failedLoginAttempts
    };
}
