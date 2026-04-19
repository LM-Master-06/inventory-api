using InventoryAPI.Models.DTOs;
using InventoryAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IInventoryService service, ILogger<InventoryController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all inventory items</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category   = null,
        [FromQuery] bool    activeOnly = true)
    {
        var items = await _service.GetAllAsync(category, activeOnly);
        return Ok(items);
    }

    /// <summary>Get item by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound(new { message = $"Item {id} not found." }) : Ok(item);
    }

    /// <summary>Get item by SKU</summary>
    [HttpGet("sku/{sku}")]
    public async Task<IActionResult> GetBySku(string sku)
    {
        var item = await _service.GetBySkuAsync(sku);
        return item is null ? NotFound(new { message = $"SKU '{sku}' not found." }) : Ok(item);
    }

    /// <summary>Search items by name, SKU, or category</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { message = "Search term cannot be empty." });

        var results = await _service.SearchAsync(term);
        return Ok(results);
    }

    /// <summary>Create a new inventory item</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var item = await _service.CreateAsync(request);
            _logger.LogInformation("Created inventory item {Id} with SKU {Sku}", item.Id, item.Sku);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create inventory item");
            return StatusCode(500, new { message = "An error occurred while creating the item." });
        }
    }

    /// <summary>Update an existing inventory item</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var item = await _service.UpdateAsync(id, request);
        if (item is null) return NotFound(new { message = $"Item {id} not found." });

        _logger.LogInformation("Updated inventory item {Id}", id);
        return Ok(item);
    }

    /// <summary>Soft-delete an inventory item</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = $"Item {id} not found." });

        _logger.LogInformation("Deleted inventory item {Id}", id);
        return NoContent();
    }

    /// <summary>Get inventory summary (total stock + total value)</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var totalStock = await _service.GetTotalStockAsync();
        var totalValue = await _service.GetTotalValueAsync();
        return Ok(new { totalStock, totalValue });
    }
}
