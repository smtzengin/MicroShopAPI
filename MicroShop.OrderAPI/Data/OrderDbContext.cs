using MicroShop.OrderAPI.Entities;
using MicroShop.OrderAPI.Models;
using MicroShop.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroShop.OrderAPI.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Para Hassasiyeti
        modelBuilder.Entity<Order>().Property(o => o.TotalPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasPrecision(18, 2);
        // 2. Adres (Owned Entity) 
        modelBuilder.Entity<Order>().OwnsOne(o => o.ShippingAddress, a =>
        {
            a.Property(p => p.Line).HasColumnName("AddressLine");
            a.Property(p => p.City).HasColumnName("AddressCity");
            a.Property(p => p.District).HasColumnName("AddressDistrict");
            a.Property(p => p.ZipCode).HasColumnName("AddressZipCode");
        });

        // 3. İlişki (Order -> OrderItems)
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow; 
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}