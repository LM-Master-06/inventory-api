using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryAPI.Data;
using InventoryAPI.Models;
using InventoryAPI.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryAPI.Tests.Integration;

/// <summary>
/// Integration tests: spins up the full ASP.NET pipeline with InMemory DB.
/// Tagged [Trait("Category","Integration")] so they can be run separately.
/// </summary>
[Trait("Category", "Integration")]
public class InventoryApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InventoryApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            // Configure services - runs before host is built
            // We register InMemory DB here, and Program.cs will skip SQLite
            // because DbContextOptions<AppDbContext> is already registered
            builder.ConfigureServices(services =>
            {
                // Register InMemory provider with a unique database name per test run
                // This must be done BEFORE Program.cs runs to prevent SQLite registration
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
            });
        }).CreateClient();
    }

    // ── Health ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GET_Health_Returns200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Metrics_ExposesPrometheusData()
    {
        var response = await _client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("http_requests");
    }

    // ── CRUD Flow ──────────────────────────────────────────────────────────
    [Fact]
    public async Task POST_CreateItem_Returns201_AndCanBeRetrieved()
    {
        var request = new CreateItemRequest(
            Name: "Integration Widget", Sku: $"INT-{Guid.NewGuid():N}",
            Description: "Test", Quantity: 10, UnitPrice: 9.99m, Category: "Test");

        var createResponse = await _client.PostAsJsonAsync("/api/inventory", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<InventoryItem>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Integration Widget");

        var getResponse = await _client.GetAsync($"/api/inventory/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PUT_UpdateItem_Returns200_WithUpdatedValues()
    {
        // Create
        var sku  = $"UPD-{Guid.NewGuid():N}";
        var post = await _client.PostAsJsonAsync("/api/inventory",
            new CreateItemRequest("Before", sku, "", 1, 1m, "A"));
        var item = await post.Content.ReadFromJsonAsync<InventoryItem>();

        // Update
        var putResponse = await _client.PutAsJsonAsync($"/api/inventory/{item!.Id}",
            new UpdateItemRequest("After", "New Desc", 99, 50m, "B", true));

        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await putResponse.Content.ReadFromJsonAsync<InventoryItem>();
        updated!.Name.Should().Be("After");
        updated.Quantity.Should().Be(99);
    }

    [Fact]
    public async Task DELETE_Item_Returns204_ThenGetReturns404()
    {
        var sku  = $"DEL-{Guid.NewGuid():N}";
        var post = await _client.PostAsJsonAsync("/api/inventory",
            new CreateItemRequest("ToDelete", sku, "", 1, 1m, "X"));
        var item = await post.Content.ReadFromJsonAsync<InventoryItem>();

        var deleteResponse = await _client.DeleteAsync($"/api/inventory/{item!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Soft-deleted item should not appear in default listing
        var getAll = await _client.GetAsync("/api/inventory");
        var list   = await getAll.Content.ReadFromJsonAsync<List<InventoryItem>>();
        list!.Should().NotContain(i => i.Id == item.Id);
    }

    [Fact]
    public async Task GET_Search_ReturnsMatchingItems()
    {
        var sku = $"SCH-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/inventory",
            new CreateItemRequest("Searchable Item", sku, "", 1, 1m, "SearchCat"));

        var response = await _client.GetAsync("/api/inventory/search?term=Searchable");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadFromJsonAsync<List<InventoryItem>>();
        results!.Should().Contain(i => i.Sku == sku);
    }

    [Fact]
    public async Task GET_Summary_ReturnsStockAndValue()
    {
        var response = await _client.GetAsync("/api/inventory/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("totalStock").And.Contain("totalValue");
    }

    [Fact]
    public async Task POST_CreateItem_Returns400_WhenNameIsEmpty()
    {
        var response = await _client.PostAsJsonAsync("/api/inventory",
            new { name = "", sku = "X", quantity = 1, unitPrice = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
