using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Order.Api.Clients.Catalog;
using Order.Api.Data;
using Order.Api.Infrastructure;
using Order.Api.Messaging;
using Order.Api.Saga;
using Order.Api.Services;
using Store.Contracts.Events;
using Store.Messaging;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStoreObservability();

var connectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("Connection string 'OrderDb' is not configured.");
builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
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

// OrderPlaced is published with publisher confirms to start the saga.
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<OrderPlacementService>();
builder.Services.AddScoped<OrderSagaOrchestrator>();

// Saga replies: one durable queue per event type, manual ack, retries, dead-letter queue, idempotency.
builder.Services.AddEventConsumer<StockReserved, StockReservedHandler>(OrderQueues.StockReserved);
builder.Services.AddEventConsumer<StockReservationFailed, StockReservationFailedHandler>(OrderQueues.StockReservationFailed);
builder.Services.AddEventConsumer<StockReleased, StockReleasedHandler>(OrderQueues.StockReleased);
builder.Services.AddEventConsumer<PaymentCharged, PaymentChargedHandler>(OrderQueues.PaymentCharged);
builder.Services.AddEventConsumer<PaymentFailed, PaymentFailedHandler>(OrderQueues.PaymentFailed);
builder.Services.AddEventConsumer<PaymentRefunded, PaymentRefundedHandler>(OrderQueues.PaymentRefunded);

// Catalog is called synchronously through the generated client with a resilience pipeline.
builder.Services.AddCatalogClient(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>("order-db")
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
