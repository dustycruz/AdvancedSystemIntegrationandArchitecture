using Inventory.Api.Clients.Catalog;
using Inventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStoreObservability();

var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException("Connection string 'InventoryDb' is not configured.");
builder.Services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));

// Catalog is called synchronously through the generated client with a resilience pipeline.
builder.Services.AddCatalogClient(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InventoryDbContext>("inventory-db");

var app = builder.Build();

app.UseStoreObservability();
app.MapHealthChecks("/health");

await app.InitializeDatabaseAsync();

app.Run();
