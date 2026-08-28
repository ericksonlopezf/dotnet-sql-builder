// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>
/// Represents an immutable audit trail record for entity changes.
/// Used for compliance, debugging, and change tracking.
/// Maps to: audit_logs table
/// </summary>
[SqlEntity("audit_logs")]
public partial class AuditLog
{
    [DatabaseGenerated]
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;  // "Customer", "Order", etc.
    public string EntityId { get; set; } = string.Empty;    // The PK value as string
    public string Action { get; set; } = string.Empty;      // "INSERT", "UPDATE", "DELETE"
    public string? OldValues { get; set; }                  // JSON snapshot before change
    public string? NewValues { get; set; }                  // JSON snapshot after change
    public string? ChangedFields { get; set; }              // JSON array of changed field names
    public int? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }              // Trace/Request ID
}


