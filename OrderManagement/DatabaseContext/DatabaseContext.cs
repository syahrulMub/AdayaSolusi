using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;
namespace OrderManagement;

public class DatabaseContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<ProductOrder> ProductOrders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<StatusOrder> StatusOrders { get; set; }
    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductOrder>()
            .HasKey(po => new { po.ProductId, po.OrderId });

        modelBuilder.Entity<ProductOrder>()
            .HasOne(po => po.Product)
            .WithMany(p => p.ProductOrders) // tambahkan collection di Product
            .HasForeignKey(po => po.ProductId);

        modelBuilder.Entity<ProductOrder>()
            .HasOne(po => po.Order)
            .WithMany(o => o.ProductOrders)
            .HasForeignKey(po => po.OrderId);

        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(x => x.Key)
            .IsUnique();

    }
}
