using FluentAssertions;
using InventoryAPI.Controllers;
using InventoryAPI.Models;
using InventoryAPI.Models.DTOs;
using InventoryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InventoryAPI.Tests.Unit;

public class InventoryControllerTests
{
    private readonly Mock<IInventoryService>  _mockService;
    private readonly InventoryController      _sut;

    public InventoryControllerTests()
    {
        _mockService = new Mock<IInventoryService>();
        _sut         = new InventoryController(_mockService.Object, NullLogger<InventoryController>.Instance);
    }

    private static InventoryItem MakeItem(string name = "Widget", string sku = "W-001") => new()
    {
        Id       = Guid.NewGuid(),
        Name     = name,
        Sku      = sku,
        Category = "Test",
        Quantity = 5,
        UnitPrice = 10m
    };

    // ── GetAll ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetAll_Returns200WithItems()
    {
        _mockService.Setup(s => s.GetAllAsync(null, true))
                    .ReturnsAsync(new[] { MakeItem() });

        var result = await _sut.GetAll();

        result.Should().BeOfType<OkObjectResult>()
              .Which.StatusCode.Should().Be(200);
    }

    // ── GetById ────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetById_Returns200_WhenFound()
    {
        var item = MakeItem();
        _mockService.Setup(s => s.GetByIdAsync(item.Id)).ReturnsAsync(item);

        var result = await _sut.GetById(item.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _mockService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((InventoryItem?)null);

        var result = await _sut.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>()
              .Which.StatusCode.Should().Be(404);
    }

    // ── Search ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Search_Returns400_WhenTermIsEmpty()
    {
        var result = await _sut.Search(string.Empty);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_Returns200_WithResults()
    {
        _mockService.Setup(s => s.SearchAsync("widget"))
                    .ReturnsAsync(new[] { MakeItem() });

        var result = await _sut.Search("widget");

        result.Should().BeOfType<OkObjectResult>();
    }

    // ── Create ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Create_Returns201_WithCreatedItem()
    {
        var request = new CreateItemRequest("Widget", "W-999", "Desc", 10, 9.99m, "Widgets");
        var created = MakeItem("Widget", "W-999");
        _mockService.Setup(s => s.CreateAsync(request)).ReturnsAsync(created);

        var result = await _sut.Create(request);

        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.StatusCode.Should().Be(201);
    }

    // ── Update ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Update_Returns200_WhenSuccessful()
    {
        var id      = Guid.NewGuid();
        var request = new UpdateItemRequest("Updated", "Desc", 5, 1m, "Cat", true);
        _mockService.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(MakeItem());

        var result = await _sut.Update(id, request);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Returns404_WhenItemDoesNotExist()
    {
        var request = new UpdateItemRequest("X", "Y", 1, 1m, "Z", true);
        _mockService.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), request))
                    .ReturnsAsync((InventoryItem?)null);

        var result = await _sut.Update(Guid.NewGuid(), request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Delete ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Delete_Returns204_WhenSuccessful()
    {
        _mockService.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _sut.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Summary ────────────────────────────────────────────────────────────
    [Fact]
    public async Task Summary_Returns200_WithStockAndValue()
    {
        _mockService.Setup(s => s.GetTotalStockAsync()).ReturnsAsync(100);
        _mockService.Setup(s => s.GetTotalValueAsync()).ReturnsAsync(500m);

        var result = await _sut.Summary();

        result.Should().BeOfType<OkObjectResult>();
    }
}
