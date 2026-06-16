using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using EFCoreChangeTracking.Core.DbContexts;
using EFCoreChangeTracking.Core.Models;

namespace EFCoreChangeTracking.Auditing;

public class ChangeTrackingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ChangeTrackingService> _logger;

    public ChangeTrackingService(ApplicationDbContext dbContext, ILogger<ChangeTrackingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets all entities currently being tracked by the change tracker
    /// </summary>
    public IEnumerable<EntityEntry> GetTrackedEntities()
    {
        return _dbContext.ChangeTracker.Entries();
    }

    /// <summary>
    /// Gets entities in a specific state
    /// </summary>
    public IEnumerable<EntityEntry> GetEntitiesByState(EntityState state)
    {
        return _dbContext.ChangeTracker.Entries().Where(e => e.State == state);
    }

    /// <summary>
    /// Detects changes and returns change summary
    /// </summary>
    public ChangeSummary GetChangeSummary()
    {
        _dbContext.ChangeTracker.DetectChanges();

        var summary = new ChangeSummary
        {
            Added = GetEntitiesByState(EntityState.Added).Count(),
            Modified = GetEntitiesByState(EntityState.Modified).Count(),
            Deleted = GetEntitiesByState(EntityState.Deleted).Count(),
            Unchanged = GetEntitiesByState(EntityState.Unchanged).Count(),
            Detached = GetEntitiesByState(EntityState.Detached).Count(),
            HasChanges = _dbContext.ChangeTracker.HasChanges()
        };

        return summary;
    }

    /// <summary>
    /// Gets detailed information about property changes for a specific entity
    /// </summary>
    public EntityChanges GetEntityChanges(EntityEntry entry)
    {
        var changes = new EntityChanges
        {
            EntityType = entry.Entity.GetType().Name,
            EntityState = entry.State,
            Timestamp = DateTime.UtcNow
        };

        foreach (var property in entry.Properties)
        {
            if (entry.State == EntityState.Modified)
            {
                if (property.IsModified)
                {
                    changes.PropertyChanges.Add(new PropertyChange
                    {
                        PropertyName = property.Metadata.Name,
                        OriginalValue = property.OriginalValue?.ToString(),
                        CurrentValue = property.CurrentValue?.ToString(),
                        PropertyType = property.Metadata.ClrType.Name
                    });
                }
            }
            else if (entry.State == EntityState.Added)
            {
                changes.PropertyChanges.Add(new PropertyChange
                {
                    PropertyName = property.Metadata.Name,
                    OriginalValue = null,
                    CurrentValue = property.CurrentValue?.ToString(),
                    PropertyType = property.Metadata.ClrType.Name
                });
            }
            else if (entry.State == EntityState.Deleted)
            {
                changes.PropertyChanges.Add(new PropertyChange
                {
                    PropertyName = property.Metadata.Name,
                    OriginalValue = property.OriginalValue?.ToString(),
                    CurrentValue = null,
                    PropertyType = property.Metadata.ClrType.Name
                });
            }
        }

        return changes;
    }

    /// <summary>
    /// Gets all property changes for all tracked entities
    /// </summary>
    public IEnumerable<EntityChanges> GetAllChanges()
    {
        _dbContext.ChangeTracker.DetectChanges();
        
        var allEntries = _dbContext.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached);

        foreach (var entry in allEntries)
        {
            yield return GetEntityChanges(entry);
        }
    }

    /// <summary>
    /// Prints debug information about tracked entities
    /// </summary>
    public void PrintTrackedEntitiesDebugInfo()
    {
        var debugInfo = _dbContext.ChangeTracker.ToDebugString();
        _logger.LogInformation("Tracked Entities Debug Info:\n{DebugInfo}", debugInfo);
    }

    /// <summary>
    /// Gets information about owned entities
    /// </summary>
    public IEnumerable<OwnedEntityInfo> GetOwnedEntityInfo()
    {
        var entries = _dbContext.ChangeTracker.Entries();
        
        foreach (var entry in entries)
        {
            var entityType = entry.Metadata;
            var ownedTypes = entityType.GetNavigations().Where(n => n.TargetEntityType.IsOwned());

            foreach (var ownedNav in ownedTypes)
            {
                yield return new OwnedEntityInfo
                {
                    OwnerType = entityType.Name,
                    OwnedType = ownedNav.TargetEntityType.Name,
                    NavigationName = ownedNav.Name,
                    IsCollection = ownedNav.IsCollection
                };
            }
        }
    }
}

public class ChangeSummary
{
    public int Added { get; set; }
    public int Modified { get; set; }
    public int Deleted { get; set; }
    public int Unchanged { get; set; }
    public int Detached { get; set; }
    public bool HasChanges { get; set; }

    public override string ToString() => 
        $"Added: {Added}, Modified: {Modified}, Deleted: {Deleted}, Unchanged: {Unchanged}, Detached: {Detached}";
}

public class EntityChanges
{
    public string EntityType { get; set; } = string.Empty;
    public EntityState EntityState { get; set; }
    public DateTime Timestamp { get; set; }
    public List<PropertyChange> PropertyChanges { get; set; } = new();
}

public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? CurrentValue { get; set; }
    public string PropertyType { get; set; } = string.Empty;

    public override string ToString() => 
        $"{PropertyName} ({PropertyType}): {OriginalValue ?? "<null>"} → {CurrentValue ?? "<null>"}";
}

public class OwnedEntityInfo
{
    public string OwnerType { get; set; } = string.Empty;
    public string OwnedType { get; set; } = string.Empty;
    public string NavigationName { get; set; } = string.Empty;
    public bool IsCollection { get; set; }
}
