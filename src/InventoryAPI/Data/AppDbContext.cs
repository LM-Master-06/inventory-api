using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");

            // Seed data
            entity.HasData(
                new InventoryItem
                {
                    Id          = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name        = "Widget A",
                    Sku         = "WGT-001",
                    Description = "Standard widget",
                    Quantity    = 100,
                    UnitPrice   = 9.99m,
                    Category    = "Widgets",
                    CreatedAt   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new InventoryItem
                {
                    Id          = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name        = "Gadget B",
                    Sku         = "GDG-001",
                    Description = "Advanced gadget",
                    Quantity    = 50,
                    UnitPrice   = 49.99m,
                    Category    = "Gadgets",
                    CreatedAt   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}
