// EF Core 11 (optional section): a tiny SQLite model for FullJoin and MaxBy/MinBy.

using Microsoft.EntityFrameworkCore;

namespace Showcase.Api.Data;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<Order> Orders { get; set; } = [];
}

public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public int? CustomerId { get; set; } // nullable: "guest" orders have no customer
    public Customer? Customer { get; set; }
}

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite has no native decimal type; store as REAL so ORDER BY/MAX work in SQL.
        modelBuilder.Entity<Order>().Property(o => o.Total).HasConversion<double>();
    }
}
