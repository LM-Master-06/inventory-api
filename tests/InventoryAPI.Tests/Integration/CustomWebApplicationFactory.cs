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
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Set environment to Testing BEFORE building the host
        // This causes Program.cs to skip SQLite registration
        builder.UseEnvironment("Testing");

        // Register InMemory database
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });

        return base.CreateHost(builder);
    }
}
