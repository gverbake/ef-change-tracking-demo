using System;
using System.Collections.Generic;

namespace EFCoreChangeTracking.Core.Models;

/// <summary>
/// Customer entity with owned address
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; } = true;

    // Owned entity
    public Address Address { get; set; } = new();

    // Contact info - owned collection
    public ICollection<ContactInfo> ContactMethods { get; set; } = new List<ContactInfo>();

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}

/// <summary>
/// Owned entity - Address (Value Object)
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public override string ToString() => $"{Street}, {City} {PostalCode}, {Country}";
}

/// <summary>
/// Owned entity collection - Contact Information
/// </summary>
public class ContactInfo
{
    public string Type { get; set; } = string.Empty; // "Phone", "Mobile", "Fax"
    public string Value { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }

    public override string ToString() => $"{Type}: {Value}";
}
