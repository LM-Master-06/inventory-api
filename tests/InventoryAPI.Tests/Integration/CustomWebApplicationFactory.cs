using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InventoryAPI.Data;

namespace InventoryAPI.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that configures Testing environment with InMemory DB.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Static constructor runs before any instance is created
    // This ensures the environment is set BEFORE Program.cs executes
    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    // Shared database name for all requests from this factory instance
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Register InMemory database for testing
        // Uses instance field so all requests share the same database
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        return base.CreateHost(builder);
    }
}
