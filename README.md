# Entity Framework Core Change Tracking Demo

A comprehensive demonstration of Entity Framework Core change tracking capabilities, including:
- **Change tracking fundamentals**
- **Auditing patterns** with change history
- **Owned entities** and their change tracking
- **Change detection strategies**
- **Practical examples** with SQL Server

## Projects Structure

- `EFCoreChangeTracking.Core` - Domain models and DbContext
- `EFCoreChangeTracking.Auditing` - Auditing service and interceptors
- `EFCoreChangeTracking.Demo` - Console application with examples

## Key Concepts Covered

### 1. Change Tracking Basics
- Entity states (Added, Modified, Unchanged, Deleted, Detached)
- ChangeTracker API
- Auto-detect changes
- Query tracking behavior

### 2. Owned Entities
- Single owned entities (OwnsOne)
- Collections of owned entities (OwnsMany)
- Change tracking with owned types
- Value objects and aggregates

### 3. Auditing
- Tracking property changes
- Recording original and current values
- Audit trail with timestamps
- User attribution
- Soft deletes with audit logging

### 4. Change Detection Strategies
- Snapshot tracking
- Change notification
- Notification with original values

## Running the Demo

```bash
cd EFCoreChangeTracking.Demo
dotnet run
```

## Database Setup

Update the connection string in `appsettings.json` and run migrations:

```bash
dotnet ef database update
```

## Examples Included

1. **Basic Change Tracking** - Track entity modifications
2. **Owned Entities** - Using value objects and aggregates
3. **Change Detection** - Detecting changes before SaveChanges
4. **Audit Logging** - Complete audit trail with change history
5. **Soft Deletes** - Logical deletion with audit tracking
6. **Complex Objects** - Nested owned entities with auditing
