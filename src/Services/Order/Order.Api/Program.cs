using Order.Api.Clients.Catalog;
using Order.Api.Data;
using Microsoft.EntityFrameworkCore;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStoreObservability();

var connectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("Connection string 'OrderDb' is not configured.");
builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));

// Catalog is called synchronously through the generated client with a resilience pipeline.
builder.Services.AddCatalogClient(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>("order-db");

var app = builder.Build();

app.UseStoreObservability();
app.MapHealthChecks("/health");

await app.InitializeDatabaseAsync();

app.Run();
