using InventoryAPI.Data;
using InventoryAPI.Models;
using InventoryAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<InventoryItem>> GetAllAsync(string? category = null, bool activeOnly = true)
    {
        var query = _db.InventoryItems.AsQueryable();

        if (activeOnly)
            query = query.Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(i => i.Category.ToLower() == category.ToLower());

        return await query.OrderBy(i => i.Name).ToListAsync();
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id) =>
        await _db.InventoryItems.FindAsync(id);

    public async Task<InventoryItem?> GetBySkuAsync(string sku) =>
        await _db.InventoryItems.FirstOrDefaultAsync(i => i.Sku == sku);

    public async Task<IEnumerable<InventoryItem>> SearchAsync(string term)
    {
        var lower = term.ToLower();
        return await _db.InventoryItems
            .Where(i => i.IsActive &&
                        (i.Name.ToLower().Contains(lower) ||
                         i.Sku.ToLower().Contains(lower)  ||
                         i.Category.ToLower().Contains(lower)))
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<InventoryItem> CreateAsync(CreateItemRequest request)
    {
        var item = new InventoryItem
        {
            Name        = request.Name,
            Sku         = request.Sku,
            Description = request.Description,
            Quantity    = request.Quantity,
            UnitPrice   = request.UnitPrice,
            Category    = request.Category
        };

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<InventoryItem?> UpdateAsync(Guid id, UpdateItemRequest request)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null) return null;

        item.Name        = request.Name;
        item.Description = request.Description;
        item.Quantity    = request.Quantity;
        item.UnitPrice   = request.UnitPrice;
        item.Category    = request.Category;
        item.IsActive    = request.IsActive;
        item.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item is null) return false;

        // Soft delete
        item.IsActive  = false;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalStockAsync() =>
        await _db.InventoryItems.Where(i => i.IsActive).SumAsync(i => i.Quantity);

    public async Task<decimal> GetTotalValueAsync() =>
        await _db.InventoryItems
            .Where(i => i.IsActive)
            .SumAsync(i => i.Quantity * i.UnitPrice);
}
