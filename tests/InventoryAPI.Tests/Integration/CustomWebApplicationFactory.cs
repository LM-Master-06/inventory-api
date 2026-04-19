using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InventoryAPI.Data;

namespace InventoryAPI.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that configures Testing environment with InMemory DB.
/// Uses a unique static database name to ensure data persists across requests.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Static database name shared across all requests from this factory instance
    private static string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Set environment to Testing BEFORE building
        builder.UseEnvironment("Testing");

        // Register InMemory database for testing
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        return base.CreateHost(builder);
    }
}
