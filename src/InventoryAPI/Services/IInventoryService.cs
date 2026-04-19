using InventoryAPI.Models;
using InventoryAPI.Models.DTOs;

namespace InventoryAPI.Services;

public interface IInventoryService
{
    Task<IEnumerable<InventoryItem>> GetAllAsync(string? category = null, bool activeOnly = true);
    Task<InventoryItem?>             GetByIdAsync(Guid id);
    Task<InventoryItem?>             GetBySkuAsync(string sku);
    Task<IEnumerable<InventoryItem>> SearchAsync(string term);
    Task<InventoryItem>              CreateAsync(CreateItemRequest request);
    Task<InventoryItem?>             UpdateAsync(Guid id, UpdateItemRequest request);
    Task<bool>                       DeleteAsync(Guid id);
    Task<int>                        GetTotalStockAsync();
    Task<decimal>                    GetTotalValueAsync();
}
