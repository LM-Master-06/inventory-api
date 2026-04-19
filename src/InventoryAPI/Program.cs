using InventoryAPI.Data;
using InventoryAPI.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Inventory Management API",
        Version     = "v1",
        Description = "REST API for managing product inventory — SIT223/SIT753 HD Task"
    });
    // Include XML comments if file exists (may not exist in test environment)
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "InventoryAPI.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile);
    }
});

// Database: Only register if DbContext hasn't been registered already
// (allows tests to override with InMemory database)
if (!builder.Services.Any(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(
            builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=/data/inventory.db"));
}

builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "db", "ready" });

// ── App ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
    c.RoutePrefix = string.Empty; // serve Swagger at root
});

// Prometheus HTTP metrics middleware (tracks request count, duration, in-flight)
app.UseHttpMetrics();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Health + Metrics endpoints
app.MapHealthChecks("/health");
app.MapMetrics("/metrics"); // Prometheus scrape endpoint

// Auto-apply migrations / seed on startup (only for relational databases)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.EnsureCreated();
    }
}

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
