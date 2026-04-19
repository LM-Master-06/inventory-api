using System.ComponentModel.DataAnnotations;

namespace InventoryAPI.Models.DTOs;

public record CreateItemRequest(
    [Required, MinLength(2)] string Name,
    [Required]               string Sku,
                             string Description,
    [Range(0, int.MaxValue)] int    Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice,
                             string Category
);

public record UpdateItemRequest(
    [Required, MinLength(2)] string  Name,
                             string  Description,
    [Range(0, int.MaxValue)] int     Quantity,
    [Range(0, double.MaxValue)] decimal UnitPrice,
                             string  Category,
                             bool    IsActive
);
