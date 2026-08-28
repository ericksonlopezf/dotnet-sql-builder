// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Fluent test data builder for <see cref="User"/>.
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

    public static UserBuilder Create() => new();

    public UserBuilder WithId(int id) { _id = id; return this; }
    public UserBuilder WithUsername(string username) { _username = username; _email = $"{username.ToLowerInvariant()}@example.com"; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithActive(bool isActive) { _isActive = isActive; return this; }
    public UserBuilder WithFailedLoginAttempts(int count) { _failedLoginAttempts = count; return this; }
    public UserBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }

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
