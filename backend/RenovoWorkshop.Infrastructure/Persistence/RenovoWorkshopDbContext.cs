using Microsoft.EntityFrameworkCore;
using RenovoWorkshop.Domain.Entities;

namespace RenovoWorkshop.Infrastructure.Persistence;

public class RenovoWorkshopDbContext : DbContext
{
    public RenovoWorkshopDbContext(DbContextOptions<RenovoWorkshopDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceOrderHistory> ServiceOrderHistories => Set<ServiceOrderHistory>();
    public DbSet<VehicleCheckList> VehicleCheckLists => Set<VehicleCheckList>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<WhatsAppMessageLog> WhatsAppMessageLogs => Set<WhatsAppMessageLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Document).HasMaxLength(30);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Plate).IsRequired().HasMaxLength(20);
            entity.HasOne(v => v.Customer)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceOrder>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Number).IsRequired().HasMaxLength(50);
            entity.Property(s => s.EstimatedTime).HasPrecision(10, 2);
            entity.Property(s => s.Value).HasPrecision(12, 2);
            entity.HasOne(s => s.Customer)
                .WithMany(c => c.ServiceOrders)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.Vehicle)
                .WithMany(v => v.ServiceOrders)
                .HasForeignKey(s => s.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.History)
                .WithOne(h => h.ServiceOrder)
                .HasForeignKey(h => h.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceOrderHistory>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Status).IsRequired().HasMaxLength(80);
            entity.Property(h => h.ChangedBy).HasMaxLength(200);
        });

        modelBuilder.Entity<VehicleCheckList>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ResponsibleUser).HasMaxLength(200);
            entity.HasOne(c => c.Vehicle)
                .WithMany()
                .HasForeignKey(c => c.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.ServiceOrder)
                .WithMany()
                .HasForeignKey(c => c.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Code).IsRequired().HasMaxLength(50);
            entity.Property(i => i.Description).IsRequired().HasMaxLength(300);
            entity.Property(i => i.PurchaseValue).HasPrecision(12, 2);
            entity.Property(i => i.AverageValue).HasPrecision(12, 2);
            entity.Property(i => i.SaleValue).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Number).IsRequired().HasMaxLength(50);
            entity.HasOne(p => p.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(p => p.Items)
                .WithOne(i => i.PurchaseOrder)
                .HasForeignKey(i => i.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.UnitValue).HasPrecision(12, 2);
            entity.HasOne(i => i.InventoryItem)
                .WithMany()
                .HasForeignKey(i => i.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
