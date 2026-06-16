using System;

namespace EFCoreChangeTracking.Core.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public AuditAction Action { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? UserName { get; set; } = "System";

    // JSON serialized changes
    public string OriginalValues { get; set; } = "{}";
    public string CurrentValues { get; set; } = "{}";
    public string ChangedProperties { get; set; } = "[]"; // Comma-separated list
}

public enum AuditAction
{
    Created,
    Modified,
    Deleted,
    Restored
}
