using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InventoryAPI.Data;

namespace InventoryAPI.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that configures InMemory database before Program.cs runs.
/// Uses ConfigureHostConfiguration to inject services early in the pipeline.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Configure services BEFORE the host is built
        // This runs before Program.cs and allows us to set up InMemory DB
        builder.ConfigureServices(services =>
        {
            // Register InMemory provider - Program.cs will skip SQLite because
            // DbContextOptions<AppDbContext> is already registered
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });

        return base.CreateHost(builder);
    }
}
