using System.Text.Json.Serialization;
using Asp.Versioning;
using Inventory.Api.Clients.Catalog;
using Inventory.Api.Data;
using Inventory.Api.Infrastructure;
using Inventory.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Store.Contracts.Events;
using Store.Messaging;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStoreObservability();

var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException("Connection string 'InventoryDb' is not configured.");
builder.Services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddVersionedSwagger();

builder.Services.AddStoreJwtAuthentication(builder.Configuration);

// Saga participant: durable queue per consumer, manual ack, retries, dead-letter queue, idempotency.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddEventConsumer<OrderPlaced, OrderPlacedHandler>(InventoryQueues.OrderPlaced);
builder.Services.AddEventConsumer<StockReleaseRequested, StockReleaseRequestedHandler>(InventoryQueues.StockReleaseRequested);
builder.Services.AddEventConsumer<OrderConfirmed, OrderConfirmedHandler>(InventoryQueues.OrderConfirmed);

// Catalog is called synchronously through the generated client with a resilience pipeline.
builder.Services.AddCatalogClient(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InventoryDbContext>("inventory-db")
    .AddCheck<RabbitMqHealthCheck>("rabbitmq");

var app = builder.Build();

app.UseStoreObservability();

if (app.Environment.IsDevelopment())
{
    app.UseVersionedSwaggerUi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.InitializeDatabaseAsync();

app.Run();
