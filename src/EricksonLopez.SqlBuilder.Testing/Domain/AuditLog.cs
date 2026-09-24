// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents an immutable audit trail record for entity changes.</summary>
[SqlEntity("audit_logs")]
public partial class AuditLog
{
    /// <summary>Gets or sets the primary key identifier of the audit log entry.</summary>
    [DatabaseGenerated]
    public long Id { get; set; }

    /// <summary>Gets or sets the name of the entity associated with the audited action.</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Gets or sets the string representation of the modified entity identifier.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the audited database action, such as INSERT, UPDATE, or DELETE.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized JSON representation of the entity state before the change.</summary>
    public string? OldValues { get; set; }

    /// <summary>Gets or sets the serialized JSON representation of the entity state after the change.</summary>
    public string? NewValues { get; set; }

    /// <summary>Gets or sets the serialized JSON array containing the names of modified fields.</summary>
    public string? ChangedFields { get; set; }

    /// <summary>Gets or sets the identifier of the user who performed the action.</summary>
    public int? UserId { get; set; }

    /// <summary>Gets or sets the IP address of the client that originated the change.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the client user-agent string that originated the change.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the change occurred.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the distributed tracing or request correlation identifier.</summary>
    public string? CorrelationId { get; set; }
}


