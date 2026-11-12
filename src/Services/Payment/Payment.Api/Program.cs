using Payment.Api.Data;
using Microsoft.EntityFrameworkCore;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStoreObservability();

var connectionString = builder.Configuration.GetConnectionString("PaymentDb")
    ?? throw new InvalidOperationException("Connection string 'PaymentDb' is not configured.");
builder.Services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentDbContext>("payment-db");

var app = builder.Build();

app.UseStoreObservability();
app.MapHealthChecks("/health");

await app.InitializeDatabaseAsync();

app.Run();
