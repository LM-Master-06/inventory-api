using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InventoryAPI.Data;

namespace InventoryAPI.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that configures InMemory database before Program.cs runs.
/// This ensures SQLite is never registered, preventing the dual provider error.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ConfigureServices runs BEFORE Program.cs, so we can register InMemory DB first
        builder.ConfigureServices(services =>
        {
            // Register InMemory provider - Program.cs will see this and skip SQLite
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });
    }
}
