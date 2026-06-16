using EFCoreChangeTracking.Auditing;
using EFCoreChangeTracking.Core.DbContexts;
using EFCoreChangeTracking.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

// Setup Dependency Injection
var services = new ServiceCollection();

services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

// Configure DbContext
services.AddDbContext<ApplicationDbContext>(options =>
{
    // For SQL Server:
    // options.UseSqlServer("Server=.;Database=EFCoreChangeTrackingDemo;Trusted_Connection=true;");

    // For In-Memory (for demo):
    options.UseInMemoryDatabase("ChangeTrackingDemo");
});

// Add services
services.AddScoped<ChangeTrackingService>();
services.AddScoped<AuditService>();
services.AddScoped<DemoRunner>();

var serviceProvider = services.BuildServiceProvider();

// Create database schema
var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.EnsureCreatedAsync();

// Run demos
var demoRunner = serviceProvider.GetRequiredService<DemoRunner>();
await demoRunner.RunAllDemosAsync();

Console.WriteLine("\n\n=== All Demos Completed ===");
