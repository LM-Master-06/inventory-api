using FluentAssertions;
using InventoryAPI.Data;
using InventoryAPI.Models;
using InventoryAPI.Models.DTOs;
using InventoryAPI.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAPI.Tests.Unit;

/// <summary>
/// Unit tests for InventoryService using EF Core InMemory provider.
/// Each test gets its own isolated DbContext instance.
/// </summary>
public class InventoryServiceTests : IDisposable
{
    private readonly AppDbContext    _context;
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut     = new InventoryService(_context);
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    private async Task<InventoryItem> SeedItemAsync(
        string name     = "Test Widget",
        string sku      = "TST-001",
        string category = "Widgets",
        int    quantity = 10,
        decimal price   = 5.00m,
        bool   active   = true)
    {
        var item = new InventoryItem
        {
            Name      = name,
            Sku       = sku,
            Category  = category,
            Quantity  = quantity,
            UnitPrice = price,
            IsActive  = active
        };
        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    // ── GetAll ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveItems_WhenActiveOnlyIsTrue()
    {
        await SeedItemAsync(name: "Active",   sku: "A-001", active: true);
        await SeedItemAsync(name: "Inactive", sku: "A-002", active: false);

        var results = await _sut.GetAllAsync(activeOnly: true);

        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCategory_WhenCategoryProvided()
    {
        await SeedItemAsync(name: "Widget",  sku: "W-001", category: "Widgets");
        await SeedItemAsync(name: "Gadget",  sku: "G-001", category: "Gadgets");

        var results = await _sut.GetAllAsync(category: "Gadgets");

        results.Should().HaveCount(1).And.OnlyContain(i => i.Category == "Gadgets");
    }

    [Fact]
    public async Task GetAllAsync_IsCaseInsensitiveForCategory()
    {
        await SeedItemAsync(sku: "W-001", category: "Widgets");

        var results = await _sut.GetAllAsync(category: "widgets");

        results.Should().HaveCount(1);
    }

    // ── GetById ────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetByIdAsync_ReturnsItem_WhenFound()
    {
        var seeded = await SeedItemAsync();

        var result = await _sut.GetByIdAsync(seeded.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(seeded.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetBySku ───────────────────────────────────────────────────────────
    [Fact]
    public async Task GetBySkuAsync_ReturnsItem_WhenSkuExists()
    {
        await SeedItemAsync(sku: "UNIQUE-SKU");

        var result = await _sut.GetBySkuAsync("UNIQUE-SKU");

        result.Should().NotBeNull();
        result!.Sku.Should().Be("UNIQUE-SKU");
    }

    // ── Search ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task SearchAsync_ReturnsMatchingItems_ByPartialName()
    {
        await SeedItemAsync(name: "Blue Widget", sku: "BW-001");
        await SeedItemAsync(name: "Red Gadget",  sku: "RG-001");

        var results = await _sut.SearchAsync("widget");

        results.Should().HaveCount(1).And.OnlyContain(i => i.Name.Contains("Widget"));
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNoMatch()
    {
        await SeedItemAsync(sku: "W-001");

        var results = await _sut.SearchAsync("zzz-no-match");

        results.Should().BeEmpty();
    }

    // ── Create ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateAsync_PersistsAndReturnsNewItem()
    {
        var request = new CreateItemRequest("New Widget", "NW-001", "Desc", 25, 12.50m, "Widgets");

        var item = await _sut.CreateAsync(request);

        item.Id.Should().NotBe(Guid.Empty);
        item.Name.Should().Be("New Widget");
        item.IsActive.Should().BeTrue();

        var persisted = await _context.InventoryItems.FindAsync(item.Id);
        persisted.Should().NotBeNull();
    }

    // ── Update ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenItemExists()
    {
        var seeded  = await SeedItemAsync(quantity: 10, price: 5.00m);
        var request = new UpdateItemRequest("Updated Name", "New Desc", 50, 15.00m, "NewCat", true);

        var result = await _sut.UpdateAsync(seeded.Id, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Quantity.Should().Be(50);
        result.UnitPrice.Should().Be(15.00m);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenItemNotFound()
    {
        var request = new UpdateItemRequest("X", "Y", 1, 1m, "Z", true);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request);

        result.Should().BeNull();
    }

    // ── Delete ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task DeleteAsync_SoftDeletes_WhenItemExists()
    {
        var seeded = await SeedItemAsync(active: true);

        var deleted = await _sut.DeleteAsync(seeded.Id);

        deleted.Should().BeTrue();
        var item = await _context.InventoryItems.FindAsync(seeded.Id);
        item!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenItemNotFound()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── Summary ────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetTotalStockAsync_SumsActiveQuantities()
    {
        await SeedItemAsync(sku: "S-001", quantity: 10, active: true);
        await SeedItemAsync(sku: "S-002", quantity: 20, active: true);
        await SeedItemAsync(sku: "S-003", quantity: 99, active: false); // excluded

        var total = await _sut.GetTotalStockAsync();

        total.Should().Be(30);
    }

    [Fact]
    public async Task GetTotalValueAsync_CalculatesCorrectly()
    {
        await SeedItemAsync(sku: "V-001", quantity: 10, price: 2.00m); // 20
        await SeedItemAsync(sku: "V-002", quantity: 5,  price: 4.00m); // 20

        var value = await _sut.GetTotalValueAsync();

        value.Should().Be(40.00m);
    }

    public void Dispose() => _context.Dispose();
}
