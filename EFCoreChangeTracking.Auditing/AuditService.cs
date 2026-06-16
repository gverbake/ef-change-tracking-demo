using EFCoreChangeTracking.Core.DbContexts;
using EFCoreChangeTracking.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EFCoreChangeTracking.Auditing;

public class AuditService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuditService> _logger;
    private string? _currentUser;

    public AuditService(ApplicationDbContext dbContext, ILogger<AuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public void SetCurrentUser(string userId, string userName)
    {
        _currentUser = userId;
    }

    /// <summary>
    /// Creates audit logs for all changed entities
    /// </summary>
    public async Task AuditChangesAsync()
    {
        _dbContext.ChangeTracker.DetectChanges();

        var entries = _dbContext.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached)
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = CreateAuditLog(entry);
            if (auditLog != null)
            {
                _dbContext.AuditLogs.Add(auditLog);
                _logger.LogInformation(
                    "Audit log created: {Action} on {EntityName} (ID: {EntityId})",
                    auditLog.Action, auditLog.EntityName, auditLog.EntityId);
            }
        }

        // Don't call SaveChanges here - let the caller control the transaction
    }

    /// <summary>
    /// Creates a single audit log entry for an entity change
    /// </summary>
    private AuditLog? CreateAuditLog(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();
        if (primaryKey == null)
            return null;

        var entityId = primaryKey.Properties.Count == 1
            ? Convert.ToInt32(entry.CurrentValues[primaryKey.Properties[0].Name])
            : 0;

        var auditLog = new AuditLog
        {
            EntityName = entry.Entity.GetType().Name,
            EntityId = entityId,
            Action = MapEntityStateToAuditAction(entry.State),
            Timestamp = DateTime.UtcNow,
            UserId = _currentUser
        };

        // Collect changed properties
        var changedProperties = new List<string>();
        var originalValues = new Dictionary<string, object?>();
        var currentValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (entry.State == EntityState.Modified)
            {
                if (property.IsModified)
                {
                    changedProperties.Add(property.Metadata.Name);
                    originalValues[property.Metadata.Name] = property.OriginalValue;
                    currentValues[property.Metadata.Name] = property.CurrentValue;
                }
            }
            else if (entry.State == EntityState.Added)
            {
                currentValues[property.Metadata.Name] = property.CurrentValue;
            }
            else if (entry.State == EntityState.Deleted)
            {
                originalValues[property.Metadata.Name] = property.OriginalValue;
            }
        }

        // Handle owned entities
        foreach (var navigation in entry.Metadata.GetNavigations().Where(n => n.TargetEntityType.IsOwned()))
        {
            changedProperties.Add(navigation.Name);

            var ownedEntry = entry.Navigation(navigation.Name);
            if (ownedEntry != null)
            {
                if (navigation.IsCollection)
                {
                    var ownedEntries = ((IEnumerable<object>?)ownedEntry.CurrentValue)?.ToList() ?? new();
                    currentValues[navigation.Name] = ownedEntries.Count;
                }
                else
                {
                    currentValues[navigation.Name] = ownedEntry.CurrentValue?.ToString() ?? "null";
                }
            }
        }

        auditLog.OriginalValues = JsonSerializer.Serialize(originalValues);
        auditLog.CurrentValues = JsonSerializer.Serialize(currentValues);
        auditLog.ChangedProperties = string.Join(", ", changedProperties);

        return auditLog;
    }

    private AuditAction MapEntityStateToAuditAction(EntityState state) => state switch
    {
        EntityState.Added => AuditAction.Created,
        EntityState.Modified => AuditAction.Modified,
        EntityState.Deleted => AuditAction.Deleted,
        _ => throw new ArgumentException($"Unexpected entity state: {state}")
    };

    /// <summary>
    /// Gets the audit history for a specific entity
    /// </summary>
    public IQueryable<AuditLog> GetAuditHistory(string entityName, int entityId)
    {
        return _dbContext.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp);
    }

    /// <summary>
    /// Gets all audit logs for an entity type
    /// </summary>
    public IQueryable<AuditLog> GetAuditHistoryByType(string entityName)
    {
        return _dbContext.AuditLogs
            .Where(a => a.EntityName == entityName)
            .OrderByDescending(a => a.Timestamp);
    }
}
