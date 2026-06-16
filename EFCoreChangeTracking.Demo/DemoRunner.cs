using Microsoft.EntityFrameworkCore;
using EFCoreChangeTracking.Core.DbContexts;
using EFCoreChangeTracking.Core.Models;
using EFCoreChangeTracking.Auditing;

namespace EFCoreChangeTracking.Demo;

public class DemoRunner
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ChangeTrackingService _changeTrackingService;
    private readonly AuditService _auditService;
    private readonly ILogger<DemoRunner> _logger;

    public DemoRunner(
        ApplicationDbContext dbContext,
        ChangeTrackingService changeTrackingService,
        AuditService auditService,
        ILogger<DemoRunner> logger)
    {
        _dbContext = dbContext;
        _changeTrackingService = changeTrackingService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task RunAllDemosAsync()
    {
        Console.WriteLine("\n===========================================");
        Console.WriteLine("EF Core Change Tracking Demo");
        Console.WriteLine("===========================================");

        await RunDemo1_BasicChangeTracking();
        await RunDemo2_OwnedEntities();
        await RunDemo3_ChangeDetection();
        await RunDemo4_Auditing();
        await RunDemo5_SoftDeletes();
    }

    private async Task RunDemo1_BasicChangeTracking()
    {
        Console.WriteLine("\n\n========== DEMO 1: Basic Change Tracking ==========");

        // Create a new customer
        var customer = new Customer
        {
            Name = "John Doe",
            Email = "john@example.com",
            CreditLimit = 10000,
            Address = new Address
            {
                Street = "123 Main St",
                City = "New York",
                PostalCode = "10001",
                Country = "USA"
            }
        };

        Console.WriteLine("\n1. Adding new customer (not yet saved)...");
        _dbContext.Customers.Add(customer);
        
        var summary = _changeTrackingService.GetChangeSummary();
        Console.WriteLine($"Change Summary: {summary}");

        Console.WriteLine("\n2. Saving changes...");
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("✓ Saved successfully");

        // Modify the customer
        Console.WriteLine("\n3. Modifying customer...");
        customer.CreditLimit = 15000;
        customer.Address.City = "Boston";
        
        summary = _changeTrackingService.GetChangeSummary();
        Console.WriteLine($"Change Summary: {summary}");

        // Show detailed changes
        var allChanges = _changeTrackingService.GetAllChanges().ToList();
        foreach (var entityChanges in allChanges)
        {
            Console.WriteLine($"\nEntity: {entityChanges.EntityType} (State: {entityChanges.EntityState})");
            foreach (var propChange in entityChanges.PropertyChanges)
            {
                Console.WriteLine($"  {propChange}");
            }
        }

        Console.WriteLine("\n4. Saving changes...");
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("✓ Saved successfully");
    }

    private async Task RunDemo2_OwnedEntities()
    {
        Console.WriteLine("\n\n========== DEMO 2: Owned Entities ==========");

        var customer = new Customer
        {
            Name = "Jane Smith",
            Email = "jane@example.com",
            CreditLimit = 20000
        };

        // Add owned single entity
        customer.Address = new Address
        {
            Street = "456 Oak Ave",
            City = "Los Angeles",
            PostalCode = "90001",
            Country = "USA"
        };

        // Add owned collection
        customer.ContactMethods.Add(new ContactInfo { Type = "Phone", Value = "555-1234", IsPrimary = true });
        customer.ContactMethods.Add(new ContactInfo { Type = "Email", Value = "jane.alt@example.com", IsPrimary = false });
        customer.ContactMethods.Add(new ContactInfo { Type = "Mobile", Value = "555-5678", IsPrimary = false });

        Console.WriteLine("\n1. Creating customer with owned entities...");
        _dbContext.Customers.Add(customer);
        
        // Show owned entity information
        var ownedInfo = _changeTrackingService.GetOwnedEntityInfo().ToList();
        Console.WriteLine("\n2. Owned Entities Information:");
        foreach (var info in ownedInfo)
        {
            var collectionText = info.IsCollection ? "(Collection)" : "(Single)";
            Console.WriteLine($"  {info.OwnerType}.{info.NavigationName}: {info.OwnedType} {collectionText}");
        }

        Console.WriteLine("\n3. Saving...");
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("✓ Saved successfully");

        // Modify owned collection
        Console.WriteLine("\n4. Modifying owned collection (adding contact)...");
        customer.ContactMethods.Add(new ContactInfo { Type = "Fax", Value = "555-9999", IsPrimary = false });
        
        var summary = _changeTrackingService.GetChangeSummary();
        Console.WriteLine($"Change Summary: {summary}");

        Console.WriteLine("\n5. Saving...");
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("✓ Saved successfully");

        // Reload and display
        Console.WriteLine("\n6. Reloading customer with owned entities...");
        _dbContext.Entry(customer).Reload();
        Console.WriteLine($"Address: {customer.Address}");
        Console.WriteLine("Contact Methods:");
        foreach (var contact in customer.ContactMethods)
        {
            Console.WriteLine($"  - {contact}");
        }
    }

    private async Task RunDemo3_ChangeDetection()
    {
        Console.WriteLine("\n\n========== DEMO 3: Change Detection ==========");

        var customer = await _dbContext.Customers.FirstAsync();
        
        Console.WriteLine($"\n1. Original customer: {customer.Name}");
        Console.WriteLine("\n2. Modifying without SaveChanges...");
        customer.Email = "newemail@example.com";
        customer.CreditLimit = 25000;
        
        Console.WriteLine("\n3. Calling DetectChanges...");
        _dbContext.ChangeTracker.DetectChanges();
        
        var summary = _changeTrackingService.GetChangeSummary();
        Console.WriteLine($"Change Summary: {summary}");
        
        var changes = _changeTrackingService.GetAllChanges().FirstOrDefault();
        if (changes != null)
        {
            Console.WriteLine($"\nChanges detected for: {changes.EntityType}");
            foreach (var change in changes.PropertyChanges)
            {
                Console.WriteLine($"  {change}");
            }
        }

        Console.WriteLine("\n4. Discarding changes (reload from database)...");
        _dbContext.Entry(customer).Reload();
        Console.WriteLine($"Email reverted to: {customer.Email}");
    }

    private async Task RunDemo4_Auditing()
    {
        Console.WriteLine("\n\n========== DEMO 4: Auditing ==========");

        _auditService.SetCurrentUser("user123", "Demo User");

        // Create a new order
        var customer = await _dbContext.Customers.FirstAsync();
        
        Console.WriteLine("\n1. Creating new order...");
        var order = new Order
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = 1500,
            Metadata = new OrderMetadata
            {
                Source = "Web",
                Notes = "Rush order",
                ReferenceCode = "REF-001"
            }
        };
        
        order.Items.Add(new OrderItem
        {
            ProductName = "Widget",
            Quantity = 10,
            UnitPrice = 100,
            LineTotal = 1000
        });
        
        order.Items.Add(new OrderItem
        {
            ProductName = "Gadget",
            Quantity = 5,
            UnitPrice = 100,
            LineTotal = 500
        });

        _dbContext.Orders.Add(order);
        Console.WriteLine("\n2. Recording audit for creation...");
        await _auditService.AuditChangesAsync();
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("✓ Order created and audited");

        // Modify the order
        Console.WriteLine("\n3. Modifying order status...");
        order.Status = OrderStatus.Confirmed;
        order.Metadata.Notes = "Rush order - approved";
        
        Console.WriteLine("\n4. Recording audit for modification...");
        await _auditService.AuditChangesAsync();
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("✓ Order modified and audited");

        // Display audit history
        Console.WriteLine("\n5. Audit History for this order:");
        var history = await _auditService.GetAuditHistory("Order", order.Id).ToListAsync();
        foreach (var log in history)
        {
            Console.WriteLine($"\n  [{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.Action} by {log.UserName}");
            Console.WriteLine($"  Changed: {log.ChangedProperties}");
        }
    }

    private async Task RunDemo5_SoftDeletes()
    {
        Console.WriteLine("\n\n========== DEMO 5: Soft Deletes with Auditing ==========");

        _auditService.SetCurrentUser("admin", "Admin User");

        var customer = await _dbContext.Customers.FirstAsync();
        
        Console.WriteLine($"\n1. Original customer count: {await _dbContext.Customers.CountAsync()}");
        Console.WriteLine($"\n2. Soft deleting customer: {customer.Name}");
        
        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        
        Console.WriteLine("\n3. Recording audit for soft delete...");
        _dbContext.Entry(customer).State = EntityState.Modified;
        await _auditService.AuditChangesAsync();
        await _dbContext.SaveChangesAsync();
        Console.WriteLine("✓ Customer soft deleted and audited");

        // Note: Query filter automatically excludes deleted customers
        Console.WriteLine($"\n4. Customer count after soft delete (excluding deleted): {await _dbContext.Customers.CountAsync()}");
        
        // Query including deleted
        var allCustomers = await _dbContext.Customers.IgnoreQueryFilters().CountAsync();
        Console.WriteLine($"   Customer count including deleted: {allCustomers}");

        // Show audit trail
        Console.WriteLine("\n5. Audit trail for deleted customer:");
        var history = await _auditService.GetAuditHistory("Customer", customer.Id).ToListAsync();
        foreach (var log in history)
        {
            Console.WriteLine($"  [{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.Action}");
        }
    }
}
