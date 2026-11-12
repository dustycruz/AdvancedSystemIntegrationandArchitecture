using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Payment.Api.Data;
using Payment.Api.Infrastructure;
using Payment.Api.Messaging;
using Store.Contracts.Events;
using Store.Messaging;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStoreObservability();

var connectionString = builder.Configuration.GetConnectionString("PaymentDb")
    ?? throw new InvalidOperationException("Connection string 'PaymentDb' is not configured.");
builder.Services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(connectionString));

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

// Saga participant: durable queue per consumer, manual ack, retries, dead-letter queue, idempotency.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddEventConsumer<ChargePaymentRequested, ChargePaymentRequestedHandler>(PaymentQueues.ChargePaymentRequested);
builder.Services.AddEventConsumer<RefundPaymentRequested, RefundPaymentRequestedHandler>(PaymentQueues.RefundPaymentRequested);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentDbContext>("payment-db")
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
